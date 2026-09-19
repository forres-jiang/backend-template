using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Common;
using My.XXX.Service;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class AuthenticationServiceTests
{
    [TestMethod]
    public async Task LoginValidatesBeforeCallingExternalClient()
    {
        var users = new UsersClient();
        var tokens = new Issuer();
        var service = new AuthenticationService(users, new Current(), tokens, null, new Monitor());
        Assert.IsTrue((await service.Login("")).IsFailed);
        Assert.AreEqual(0, users.Calls);
        Assert.IsTrue((await service.Login("invalid")).IsFailed);
        Assert.AreEqual(1, users.Calls);
        Assert.IsNull(tokens.User);
    }

    [TestMethod]
    public async Task LoginDeduplicatesRolesAndPreservesResponseContract()
    {
        var roleId = Guid.NewGuid();
        var users = new UsersClient
        {
            User = new Users
            {
                UserId = "42",
                UserName = "Test",
                EmailAddress = "test@example.invalid",
                Roles = new() { new() { RoleID = roleId, RoleName = "Reader" }, new() { RoleID = roleId, RoleName = "Reader" } }
            }
        };
        var issuer = new Issuer();
        var service = new AuthenticationService(users, new Current(), issuer, null, new Monitor());
        var result = await service.Login("ticket");
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, issuer.User.Roles);
        Assert.HasCount(1, issuer.User.RoleIds);
        Assert.AreEqual("test@example.invalid", issuer.User.Email);
        var expected = LoginResult.Success(result.Value.User, "access", "refresh", 30);
        Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result.ToLoginResult()));
    }

    [TestMethod]
    public void RefreshRequiresIdentityAndPreservesRefreshExpiration()
    {
        var current = new Current();
        var issuer = new Issuer();
        var service = new AuthenticationService(new UsersClient(), current, issuer, null, new Monitor());
        Assert.IsTrue(service.Refresh().IsFailed);
        current.Principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Test") }, "test"));
        var result = service.Refresh();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(current.TokenExpirationTime, issuer.Expiration);
        Assert.IsNull(result.ToLoginResult().Data);
    }

    [TestMethod]
    public async Task ExternalFailurePropagatesInsteadOfBecomingInvalidCredentials()
    {
        var service = new AuthenticationService(new UsersClient { Failure = new InvalidOperationException("outage") },
            new Current(), new Issuer(), null, new Monitor());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Login("ticket"));
    }

    private sealed class UsersClient : IAppCenterService
    {
        public Users User;
        public Exception Failure;
        public int Calls;
        public Task<Users> GetUserByTicket(string ticket)
        {
            Calls++;
            if (Failure != null) throw Failure;
            return Task.FromResult(User);
        }
        public Task<string> GetToken() => throw new NotSupportedException();
        public Task<string> GetBearerToken() => throw new NotSupportedException();
        public Task<Users> GetUserById(string id) => throw new NotSupportedException();
        public Task<List<Role>> GetRole() => throw new NotSupportedException();
    }

    private sealed class Issuer : ITokenIssuer
    {
        public UserInfo User;
        public DateTime Expiration;
        public TokenPair Issue(UserInfo user)
        {
            User = user;
            return new() { AccessToken = "access", RefreshToken = "refresh", ExpiryInMinutes = 30 };
        }
        public TokenPair Refresh(UserInfo user, DateTime expiration) { Expiration = expiration; return Issue(user); }
    }

    private sealed class Current : ICurrentRequest
    {
        public ClaimsPrincipal Principal { get; set; } = new(new ClaimsIdentity());
        public UserInfo User { get; } = new() { UserId = "42" };
        public DateTime TokenExpirationTime { get; } = DateTime.UtcNow.AddDays(1);
        public string CultureName => "en-US";
    }

    private sealed class Monitor : IOptionsMonitor<AppConfig>
    {
        public AppConfig CurrentValue { get; } = new();
        public AppConfig Get(string name) => CurrentValue;
        public IDisposable OnChange(Action<AppConfig, string> listener) => null;
    }
}
