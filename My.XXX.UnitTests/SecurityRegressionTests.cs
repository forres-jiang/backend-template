using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Infrastructure;
using My.XXX.Service;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class SecurityRegressionTests
{
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
        var service = new MenuService(new TestCurrentRequest("zh-CN"), null, null, new ApplicationMapper());
        var menus = new List<MenuDto> { new() { DisplayName = "fallback", DisplayNames = "{\"zh-CN\":\"菜单\"}" } };
        service.SetMenuLanguage(menus);
        Assert.AreEqual("菜单", menus[0].DisplayName);
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

    private sealed class TestCurrentRequest(string cultureName) : ICurrentRequest
    {
        public System.Security.Claims.ClaimsPrincipal Principal => new();
        public UserInfo User => null;
        public DateTime TokenExpirationTime => DateTime.MinValue;
        public string CultureName => cultureName;
    }

    private sealed class FailingOperations : IOperationService
    {
        public Task Save(MetricsInfo request) => throw new InvalidOperationException("storage unavailable");
        public Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query) => throw new NotSupportedException();
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
