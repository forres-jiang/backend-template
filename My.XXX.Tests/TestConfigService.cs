using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using My.XXX.Infra;

namespace My.XXX.Tests
{
    public static class TestConfigService
    {
        public static void AddUserService(this IServiceCollection services, IConfiguration configuration)
        {
            //CSRedisCore
            var redisConfig = configuration.GetSection("RedisConfig").Get<RedisConfig>();
            RedisHelper.Initialization(new CSRedis.CSRedisClient(redisConfig.ConnectionString));

            services.AddMemoryCache();
            //注入HttpContext
            services.AddHttpContextAccessor();
            //AppCenter
            services.Configure<AppCenterConfig>(configuration.GetSection("AppCenterConfig"));

            //services.AddScoped<IAppCenterService, AppCenterService>();

            //var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(m => m.FullName.StartsWith("PwC"));
            //services.Scan(scan => scan.FromAssemblies(assemblies)
            //.AddClasses(classes => classes.AssignableTo<IScopeDependency>())
            //.AsImplementedInterfaces()
            //.WithScopedLifetime()

            //.AddClasses(classes => classes.AssignableTo<ITransientDependency>())
            //.AsImplementedInterfaces()
            //.WithTransientLifetime()

            //.AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
            //.AsImplementedInterfaces()
            //.WithSingletonLifetime());

            //// 获取需要使用的服务实例
            //var provider = services.BuildServiceProvider();
            //var _logService = provider.GetRequiredService<ILogService>();

            //FlurlHttp.Configure(settings =>
            //{
            //    settings.HttpClientFactory = new DefaultHttpClientFactory();
            //    settings.AfterCall = call =>
            //    {
            //        _logService.SaveLogs(new
            //        {
            //            CreateTime = DateTime.Now,
            //            call.Request.Url,
            //            call.RequestBody,
            //            HttpStatusCode = call.HttpResponseMessage?.StatusCode,
            //            ReturnValue = call.HttpResponseMessage.Content.ReadAsStringAsync(),
            //            Error = ""
            //        }, "AppCenterAPILog");
            //    };
            //    settings.OnError = call =>
            //    {
            //        logger.LogError("Error: {Message} | StackTrace: {StackTrace}", call.Exception.Message, call.Exception.StackTrace);
            //        call.ExceptionHandled = true;
            //    };
            //});
        }
    }
}