using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using My.XXX.APIs;
using My.XXX.APIs.Common;
using My.XXX.APIs.Common.Middleware;
using My.XXX.APIs.Configurations;
using My.XXX.APIs.Models;
using My.XXX.Infrastructure;
using My.XXX.Infrastructure.Caching;
using My.XXX.Infrastructure.Health;
using My.XXX.Infrastructure.Security;
using My.XXX.Persistences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            AESHelper.ValidateKey();
            var jwt = configuration.GetSection("JwtConfig").Get<JwtConfig>();
            My.XXX.APIs.Common.JWT.TokenAuthentication.Configure(new JwtBearerOptions(), jwt, "access");
            if (jwt.ExpiryInMinutes <= 0 || jwt.RefreshExpiryInMinutes <= jwt.ExpiryInMinutes)
                throw new InvalidOperationException("Refresh token lifetime must exceed the positive access token lifetime.");

            //AppConfig
            services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
            services.Configure<My.XXX.Infrastructure.Logging.RequestLogOptions>(configuration.GetSection("AppConfig"));
            //ConnStrings
            services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
            //JWT
            services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));
            services.Configure<My.XXX.Services.Authentication.Models.SessionOptions>(configuration.GetSection("JwtConfig"));
            services.AddOptions<My.XXX.Infrastructure.Caching.PermissionCacheOptions>()
                .Bind(configuration.GetSection("PermissionCache"))
                .Validate(o => o.ExpiryInMinutes > 0 && !string.IsNullOrWhiteSpace(o.KeyPrefix),
                    "PermissionCache needs a positive expiry and a key prefix.")
                .ValidateOnStart();
            //Permissions
            services.Configure<PermissionWhitelist>(configuration.GetSection("PermissionWhitelist"));
        }

        public static void AddDefaultService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services
                .AddControllers(filter =>
                {
                    //接口统一数据封装
                    filter.Filters.Add<UnifyResultAsync>();
                })
                .AddNewtonsoftJson(option =>
                {
                    //使用本地时区
                    option.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                    //日期格式
                    option.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss.fff";
                    //忽略循环引用
                    option.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(AnnotationsResource));
                });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    CultureType.en_US,
                    CultureType.zh_CN,
                    CultureType.zh_TW,
                    CultureType.zh_HK
                };

                options.SetDefaultCulture(supportedCultures.First())
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
            });

            //重写ModelState 返回类型
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    //获取验证失败的模型字段
                    var errors = actionContext.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => e.Value.Errors.First().ErrorMessage).ToList();
                    //设置返回内容,根据实际情况可以调整返回httpstatus200或400
                    if (actionContext.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ExplicitApiContractAttribute>() != null)
                        return new BadRequestObjectResult(new ApiResponse<object>(0, string.Join("|", errors), null,
                            "Request.ValidationFailed", actionContext.HttpContext.TraceIdentifier));
                    return new BadRequestObjectResult(BaseResult.Fail(string.Join("|", errors)));
                };
            });

            //权限
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
                .AddPolicy("Permissions", polic =>
                {
                    polic.RequireAuthenticatedUser();
                    polic.AddRequirements(new PermissionsRequirement(PolicyType.Default));
                });

            services.AddScoped<IAuthorizationHandler, PermissionsHandler>();

            //健康检查
            services.AddHealthChecks();

            //注入HttpContext
            services.AddHttpContextAccessor();

            services.AddDataProtection()
                .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "XXX").SetDefaultKeyLifetime(TimeSpan.FromDays(90))
                .PersistKeysToFileSystem(new DirectoryInfo(configuration["DataProtection:KeyPath"] ?? Path.Combine(AppContext.BaseDirectory, "keys")))
                .ProtectKeysWithAES();

            //身份验证
            var jwtConfig = configuration.GetSection("JwtConfig").Get<JwtConfig>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => My.XXX.APIs.Common.JWT.TokenAuthentication.Configure(options, jwtConfig, "access"))
                .AddJwtBearer("Refresh", options => My.XXX.APIs.Common.JWT.TokenAuthentication.Configure(options, jwtConfig, "refresh"));

            //跨域,根据实际情况开启
            var AllowedHostArr = configuration.GetSection("AllowedHostArray").Get<string[]>() ?? Array.Empty<string>();
            services.AddCors(options =>
            {
                options.AddPolicy("AppPolicy", builder =>
                {
                    builder.WithOrigins(AllowedHostArr).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                });
            });



            services.AddScoped<ExceptionHandlingMiddleware>();

            services.AddSwaggerGen(c =>
            {
                var doc = new OpenApiInfo { Title = "My XXX API", Version = "v1" };
                c.SwaggerDoc("v1", doc);

                c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("bearerAuth", document),
                        new List<string>()
                    }
                });

                // 反射获取xml注释文件
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    // 启用xml注释. 该方法第二个参数启用控制器的注释，默认为false. 需要关闭1591警告
                    c.IncludeXmlComments(xmlPath, true);
                }

                var serviceXml = Path.Combine(AppContext.BaseDirectory, "My.XXX.Services.xml");
                if (File.Exists(serviceXml))
                {
                    c.IncludeXmlComments(serviceXml, true);
                }
            });
        }

        private static string ResolveConnectionString(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Configure the {name} connection string through a secret provider.");
            return value.StartsWith("enc:v1:", StringComparison.Ordinal) ? AESHelper.Decrypt(value) : value;
        }

        public static void AddDBs(this IServiceCollection services, IConfiguration configuration)
        {
            var defaultProvider = DatabaseConfiguration.ParseProvider(configuration["DatabaseProviders:Default"]);
            var connStrings = configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>();

            //DB
            var defaultConnection = ResolveConnectionString(connStrings?.Default, "Default");
            services.AddPersistenceDatabases(defaultConnection, defaultProvider);
            services.AddPersistenceHealthChecks(defaultConnection, defaultProvider);

            //Redis
            var redisConfig = configuration.GetSection("RedisConfig").Get<RedisConfig>();
            var redisConnection = string.IsNullOrWhiteSpace(redisConfig?.ConnectionString) ? null
                : ResolveConnectionString(redisConfig.ConnectionString, "Redis");
            services.AddPermissionCaching(redisConnection,
                configuration.GetValue<PermissionDataCache>("AppConfig:PermissionDataCache") == PermissionDataCache.Redis);
            if (redisConnection != null)
                services.AddRedisHealthChecks();
        }
    }
}
