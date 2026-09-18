using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs;
using My.XXX.APIs.Common.JWT;
using My.XXX.APIs.Common.Middleware;
using My.XXX.Data;
using My.XXX.Infra;
using My.XXX.Infra.Common;
using My.XXX.Service;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Tests;

[TestClass]
[DoNotParallelize]
public class SecurityRegressionTests
{
    private static readonly JwtConfig Jwt = new()
    {
        Secret = "local-regression-test-signing-key-at-least-32-bytes",
        Issuer = "regression", Audience = "regression",
        ExpiryInMinutes = 5, RefreshExpiryInMinutes = 60
    };

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [TestMethod]
    public async Task FullHostStartsAndEnforcesEndpointAndTokenBoundaries()
    {
        var previousKey = Environment.GetEnvironmentVariable("APP_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("APP_ENCRYPTION_KEY", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["JwtConfig:Secret"] = Jwt.Secret,
                ["JwtConfig:Issuer"] = Jwt.Issuer,
                ["JwtConfig:Audience"] = Jwt.Audience,
                ["JwtConfig:ExpiryInMinutes"] = "5",
                ["JwtConfig:RefreshExpiryInMinutes"] = "60",
                ["AppCenterConfig:AppSecret"] = "local-test-only",
                ["AppCenterConfig:AppCode"] = "test",
                ["AppCenterConfig:LoginUrl"] = "https://example.invalid/login",
                ["ConnectionStrings:Default"] = "Server=127.0.0.1,1;Database=test;User Id=test;Password=test;Connect Timeout=1;Encrypt=false;ConnectRetryCount=0",
                ["ConnectionStrings:MailMaster"] = "Server=127.0.0.1,1;Database=test;User Id=test;Password=test;Connect Timeout=1;Encrypt=false;ConnectRetryCount=0",
                ["AllowedHostArray:0"] = "https://localhost",
                ["DataProtection:KeyPath"] = Path.Combine(Root(), ".test-artifacts", Guid.NewGuid().ToString("N")),
                ["AppConfig:EnableRequestLog"] = "false"
            };
            await using var app = Program.CreateApplication(
                new[] { "--contentRoot", Path.Combine(Root(), "My.XXX.APIs"), "--environment", "Production" },
                builder =>
                {
                    builder.Configuration.Sources.Clear();
                    builder.Configuration.AddInMemoryCollection(settings);
                    builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
                });
            await app.StartAsync();
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
                Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/healthy")).StatusCode);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/ready")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized,
                    (await client.PostAsync("/api/Operation/list", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized,
                    (await client.PostAsync("/api/Role/List", null)).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/User/Logout", null)).StatusCode);

                var actions = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;
                Assert.IsFalse(actions.OfType<ControllerActionDescriptor>().Any(action => action.ControllerName == "Demo"));
                var protector = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("regression");
                var protectedValue = protector.Protect("round-trip");
                var protectionServices = new ServiceCollection();
                protectionServices.AddDataProtection().SetApplicationName("XXX")
                    .PersistKeysToFileSystem(new DirectoryInfo(settings["DataProtection:KeyPath"])).ProtectKeysWithAES();
                using (var protectionProvider = protectionServices.BuildServiceProvider())
                {
                    var freshProtector = protectionProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("regression");
                    Assert.AreEqual("round-trip", freshProtector.Unprotect(protectedValue));
                }
                using (var first = app.Services.CreateScope())
                using (var second = app.Services.CreateScope())
                {
                    Assert.IsNotNull(first.ServiceProvider.GetRequiredService<IMenuService>());
                    Assert.AreNotSame(first.ServiceProvider.GetRequiredService<ExceptionHandlingMiddleware>(),
                        second.ServiceProvider.GetRequiredService<ExceptionHandlingMiddleware>());
                    Assert.AreNotSame(first.ServiceProvider.GetRequiredService<DBContext>(),
                        second.ServiceProvider.GetRequiredService<DBContext>());
                }

                var tokens = JwtTokenBuilder.CreateTokens(Jwt, new UserInfo
                {
                    UserId = "test-user", UserName = "Test", Email = "test@example.invalid",
                    Roles = new List<string>(), RoleIds = new List<Guid>()
                });
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/Demo/retry")).StatusCode);
                Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Forbidden,
                    (await client.PostAsync("/api/Operation/list", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
                Assert.AreEqual(HttpStatusCode.Forbidden, (await client.PostAsync("/api/Role/List", null)).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/User/RefreshToken", null)).StatusCode);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.RefreshToken);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                var refresh = await client.PostAsync("/api/User/RefreshToken", null);
                Assert.AreEqual(HttpStatusCode.OK, refresh.StatusCode);
                var refreshed = await refresh.Content.ReadAsStringAsync();
                StringAssert.Contains(refreshed, "eyJ");
            }
            finally { await app.StopAsync(); }
        }
        finally { Environment.SetEnvironmentVariable("APP_ENCRYPTION_KEY", previousKey); }
    }

    [TestMethod]
    public void ConfigurationHonorsHostEnvironmentAndExternalOverrides()
    {
        const string variable = "JwtConfig__Audience";
        var previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, "from-environment");
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Path.Combine(Root(), "My.XXX.APIs"), EnvironmentName = "Production"
            });
            Program.ConfigureSources(builder, Array.Empty<string>());
            Assert.AreEqual("from-environment", builder.Configuration["JwtConfig:Audience"]);
            Program.ConfigureSources(builder, new[] { "--JwtConfig:Audience", "from-command-line" });
            Assert.AreEqual("from-command-line", builder.Configuration["JwtConfig:Audience"]);
        }
        finally { Environment.SetEnvironmentVariable(variable, previous); }
    }

    [TestMethod]
    public void EncryptionUsesRandomNoncesAndRejectsTamperingAndMissingKeys()
    {
        var previous = Environment.GetEnvironmentVariable("APP_ENCRYPTION_KEY");
        try
        {
            Environment.SetEnvironmentVariable("APP_ENCRYPTION_KEY", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            var first = AESHelper.Encrypt("secret-value");
            var second = AESHelper.Encrypt("secret-value");
            Assert.AreNotEqual(first, second);
            Assert.AreEqual("secret-value", AESHelper.Decrypt(first));
            var payload = Convert.FromBase64String(first.Substring(7));
            payload[^1] ^= 1;
            Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => AESHelper.Decrypt("enc:v1:" + Convert.ToBase64String(payload)));
            Environment.SetEnvironmentVariable("APP_ENCRYPTION_KEY", null);
            Assert.ThrowsExactly<InvalidOperationException>(() => AESHelper.Encrypt("value"));
        }
        finally { Environment.SetEnvironmentVariable("APP_ENCRYPTION_KEY", previous); }
    }

    [TestMethod]
    public void RoleMenuAssignmentIncludesDistinctMenuAndActionIds()
    {
        var model = new RoleMenuActionModel
        {
            RoleId = Guid.NewGuid(),
            Menus = new List<MenuAction>
            {
                new() { MenuId = 1, ActionIds = new List<int> { 2, 3, 3 } },
                new() { MenuId = 4, ActionIds = null }
            }
        };
        var relations = RoleMenuRelations.Build(model, "user", DateTime.UtcNow);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, relations.Select(relation => relation.MenuId).ToArray());
        Assert.IsTrue(relations.All(relation => relation.RoleId == model.RoleId && relation.CreatedBy == "user"));
        model.Menus[0].ActionIds.Add(-1);
        Assert.ThrowsExactly<ArgumentException>(() => RoleMenuRelations.Build(model, "user", DateTime.UtcNow));
    }

    [TestMethod]
    public void MenuLocalizationAppliesNonEmptyTranslations()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture("zh-CN"), null));
        var service = new MenuService(new HttpContextAccessor { HttpContext = context },
            new Monitor<AppConfig>(new()), new Monitor<JwtConfig>(Jwt), null, NullLogger<MenuService>.Instance,
            null, null, new ApplicationMapper());
        var menus = new List<MenuDto> { new() { DisplayName = "fallback", DisplayNames = "{\"zh-CN\":\"菜单\"}" } };
        service.SetMenuLanguage(menus);
        Assert.AreEqual("菜单", menus[0].DisplayName);
    }

    [TestMethod]
    [DataRow("[]")]
    [DataRow("{invalid")]
    public async Task MalformedBodiesAndLoggingFailuresDoNotBreakExceptionResponses(string body)
    {
        var logger = new RecordingLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(new Monitor<AppCenterConfig>(new() { AppCode = "test" }), logger,
            new Monitor<AppConfig>(new() { EnableRequestLog = true, ExceptionStorageType = StorageTypeEnum.SQL }),
            new FailingOperations(), null);
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context, _ => throw new InvalidOperationException("sensitive-database-detail"));
        Assert.AreEqual(500, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        StringAssert.Contains(response, "requestId");
        Assert.IsFalse(response.Contains("sensitive-database-detail"));
        Assert.AreEqual(0L, context.Request.Body.Position);
        Assert.IsFalse(string.Join(" ", logger.Messages).Contains("sensitive-database-detail"));
    }

    [TestMethod]
    public async Task LoggingFailurePreservesSuccessfulResponse()
    {
        var middleware = new ExceptionHandlingMiddleware(new Monitor<AppCenterConfig>(new()), NullLogger<ExceptionHandlingMiddleware>.Instance,
            new Monitor<AppConfig>(new() { EnableRequestLog = true, RequestLogStorageType = StorageTypeEnum.SQL }),
            new FailingOperations(), null);
        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context, ctx => { ctx.Response.StatusCode = 201; return Task.CompletedTask; });
        Assert.AreEqual(201, context.Response.StatusCode);
    }

    [TestMethod]
    [DataRow(200)]
    [DataRow(201)]
    [DataRow(204)]
    public async Task EmptyPutKeepsMethodAndAcceptsAllSuccessfulStatusCodes(int status)
    {
        var handler = new RecordingHandler((HttpStatusCode)status);
        var logger = new RecordingLogger<HttpService>();
        var service = new HttpService(new Factory(handler), logger);
        var result = await service.Put<string>(new RequestModel { Url = "https://example.invalid/?secret=hidden", Token = "Bearer hidden" });
        Assert.AreEqual(HttpMethod.Put, handler.Method);
        Assert.AreEqual(1, result.Status);
        Assert.AreEqual((HttpStatusCode)status, result.HttpStatusCode);
        Assert.IsFalse(string.Join(" ", logger.Messages).Contains("hidden"));
    }

    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string name) => value;
        public IDisposable OnChange(Action<T, string> listener) => null;
    }

    private sealed class FailingOperations : IOperationService
    {
        public Task Save(MetricsInfo request) => throw new InvalidOperationException("storage unavailable");
        public Task<Paged<My.XXX.Data.PersistantObjects.Operation>> GetRequestLogs(OperationQeury query) => throw new NotSupportedException();
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpMethod Method { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(status == HttpStatusCode.NoContent ? "" : "{\"Status\":1,\"Data\":\"hidden\"}")
            });
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
