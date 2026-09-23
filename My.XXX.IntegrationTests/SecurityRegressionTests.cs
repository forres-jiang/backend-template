using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs;
using My.XXX.APIs.Common.JWT;
using My.XXX.APIs.Common.Middleware;
using My.XXX.APIs.Configurations;
using My.XXX.Contracts.DTOs;
using My.XXX.Persistences;
using My.XXX.Services.Authentication.Interfaces;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Menus.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace My.XXX.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class SecurityRegressionTests
{
    private static readonly JwtConfig Jwt = new()
    {
        Secret = "local-regression-test-signing-key-at-least-32-bytes",
        Issuer = "regression",
        Audience = "regression",
        ExpiryInMinutes = 5,
        RefreshExpiryInMinutes = 60
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
            var authority = new InMemoryAuthenticationStore();
            var settings = new Dictionary<string, string>
            {
                ["JwtConfig:Secret"] = Jwt.Secret,
                ["JwtConfig:Issuer"] = Jwt.Issuer,
                ["JwtConfig:Audience"] = Jwt.Audience,
                ["JwtConfig:ExpiryInMinutes"] = "5",
                ["JwtConfig:RefreshExpiryInMinutes"] = "60",
                ["ConnectionStrings:Default"] = "Server=127.0.0.1,1;Database=test;User Id=test;Password=test;Connect Timeout=1;Encrypt=false;ConnectRetryCount=0",
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
                    builder.Services.AddSingleton<IAuthenticationStore>(authority);
                    builder.Services.AddControllers().AddApplicationPart(typeof(BoundaryProbeController).Assembly);
                    builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
                });
            await app.StartAsync();
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
                Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/healthy")).StatusCode);
                foreach (var (path, status, code) in new[]
                {
                    ("/__test/boundary/explicit-failure", HttpStatusCode.NotFound, "Menu.NotFound"),
                    ("/__test/boundary/explicit-exception", HttpStatusCode.InternalServerError, "Server.Unexpected"),
                    ("/api/v2/permissions/catalog", HttpStatusCode.Unauthorized, "Authentication.Required")
                })
                {
                    var response = await client.GetAsync(path);
                    Assert.AreEqual(status, response.StatusCode);
                    var body = JObject.Parse(await response.Content.ReadAsStringAsync());
                    Assert.AreEqual(code, (string)body["code"]);
                    Assert.AreEqual(response.Headers.GetValues("X-Request-Id").Single(), (string)body["traceId"]);
                    Assert.IsFalse(body.ToString().Contains("private database detail"));
                }
                var explicitValidation = await client.PostAsync("/__test/boundary/explicit-validate", new StringContent("{}", Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.BadRequest, explicitValidation.StatusCode);
                Assert.AreEqual("Request.ValidationFailed", (string)JObject.Parse(await explicitValidation.Content.ReadAsStringAsync())["code"]);
                var success = JObject.Parse(await client.GetStringAsync("/__test/boundary/success"));
                Assert.AreEqual(1, (int)success["statusCode"]);
                Assert.AreEqual(7, (int)success["data"]["id"]);
                var failure = JObject.Parse(await client.GetStringAsync("/__test/boundary/failure"));
                Assert.AreEqual(0, (int)failure["statusCode"]);
                var invalid = await client.PostAsync("/__test/boundary/validate", new StringContent("{}", Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
                Assert.AreEqual(0, (int)JObject.Parse(await invalid.Content.ReadAsStringAsync())["statusCode"]);
                var exception = await client.GetAsync("/__test/boundary/exception");
                Assert.AreEqual(HttpStatusCode.InternalServerError, exception.StatusCode);
                var exceptionJson = JObject.Parse(await exception.Content.ReadAsStringAsync());
                Assert.AreEqual("0", (string)exceptionJson["state"]);
                Assert.IsNotNull(exceptionJson["requestId"]);
                Assert.IsFalse(exceptionJson.ToString().Contains("private database detail"));
                var raw = JObject.Parse(await client.GetStringAsync("/__test/boundary/raw"));
                Assert.AreEqual(7, (int)raw["id"]);
                Assert.IsNull(raw["statusCode"]);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/ready")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized,
                    (await client.PostAsync("/api/Operation/list", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized,
                    (await client.PostAsync("/api/Menu/Tree", null)).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/User/Session")).StatusCode);

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

                    Assert.IsNotNull(first.ServiceProvider.GetRequiredService<IAuthenticationService>());
                    Assert.IsNotNull(first.ServiceProvider.GetRequiredService<IPermissionQuery>());
                    Assert.AreNotSame(first.ServiceProvider.GetRequiredService<ExceptionHandlingMiddleware>(),
                        second.ServiceProvider.GetRequiredService<ExceptionHandlingMiddleware>());
                    Assert.AreNotSame(first.ServiceProvider.GetRequiredService<DBContext>(),
                        second.ServiceProvider.GetRequiredService<DBContext>());
                }

                var user = new UserInfo
                {
                    UserId = "test-user",
                    UserName = "Test",
                    Email = "test@example.invalid",
                    Roles = new List<string>(),
                    RoleIds = new List<Guid>()
                };
                await authority.SetUserAsync(user, true);
                using var tokenScope = app.Services.CreateScope();
                var issuer = tokenScope.ServiceProvider.GetRequiredService<ISessionService>();
                var tokens = await issuer.IssueAsync(user.UserId);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/Demo/retry")).StatusCode);
                Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Forbidden,
                    (await client.PostAsync("/api/Operation/list", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
                Assert.AreEqual(HttpStatusCode.Forbidden, (await client.PostAsync("/api/Menu/Tree", null)).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/User/RefreshToken", null)).StatusCode);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.RefreshToken);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                var refresh = await client.PostAsync("/api/User/RefreshToken", null);
                Assert.AreEqual(HttpStatusCode.OK, refresh.StatusCode);
                var refreshed = await refresh.Content.ReadAsStringAsync();
                StringAssert.Contains(refreshed, "eyJ");
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/User/RefreshToken", null)).StatusCode,
                    "A refresh token must be consumed only once.");
                var refreshedJson = JObject.Parse(refreshed);
                var nextAccess = (string)refreshedJson["accessToken"];
                var nextRefresh = (string)refreshedJson["refreshToken"];
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nextAccess);
                user.Roles = new() { "AppAdmin" };
                await authority.SetUserAsync(user, true);
                Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/v2/permissions/catalog")).StatusCode);
                user.Roles.Clear();
                await authority.SetUserAsync(user, true);
                Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v2/permissions/catalog")).StatusCode,
                    "An old access token must not preserve administrator access.");
                await authority.SetUserAsync(user, false);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                await authority.SetUserAsync(user, true);
                Assert.AreEqual(HttpStatusCode.OK, (await client.DeleteAsync("/api/User/Session")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nextRefresh);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/User/RefreshToken", null)).StatusCode);
                // 即使旧式令牌签名正确，在没有已持久化会话的情况下仍会被拒绝。
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTokenBuilder.CreateTokens(Jwt, user).AccessToken);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/GetRoles")).StatusCode);
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
                ContentRootPath = Path.Combine(Root(), "My.XXX.APIs"),
                EnvironmentName = "Production"
            });
            Program.ConfigureSources(builder, Array.Empty<string>());
            Assert.AreEqual("from-environment", builder.Configuration["JwtConfig:Audience"]);
            Program.ConfigureSources(builder, new[] { "--JwtConfig:Audience", "from-command-line" });
            Assert.AreEqual("from-command-line", builder.Configuration["JwtConfig:Audience"]);
        }
        finally { Environment.SetEnvironmentVariable(variable, previous); }
    }
}
