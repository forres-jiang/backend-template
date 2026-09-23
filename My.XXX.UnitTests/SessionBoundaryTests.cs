using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Authentication;
using My.XXX.Services.Authentication.Interfaces;
using My.XXX.Services.Authentication.Models;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Shared;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class SessionBoundaryTests
{
    [TestMethod]
    public async Task ValidationRejectsMismatchedIdentitiesTokensAndExpiredSessions()
    {
        var store = new Store();
        var sessions = Create(store);
        Assert.IsTrue((await sessions.ValidateAsync("session", "user")).IsSuccess);
        Assert.IsTrue((await sessions.ValidateAsync("session", "user", "refresh")).IsSuccess);
        foreach (var input in new[] { ("", "user", "refresh"), ("session", "other", "refresh"), ("session", "user", "old"), ("session", "user", "") })
        {
            var failure = await sessions.ValidateAsync(input.Item1, input.Item2, input.Item3);
            Assert.IsTrue(failure.IsFailed);
            Assert.AreEqual("Authentication.InvalidToken", failure.Errors.OfType<BusinessError>().Single().Code);
        }
        store.Session.ExpiresUtc = DateTime.UnixEpoch;
        Assert.IsTrue((await sessions.ValidateAsync("session", "user")).IsFailed);
    }

    [TestMethod]
    public async Task RefreshStillRejectsAnAtomicRotationLostAfterValidation()
    {
        var store = new Store { RejectRotation = true };
        var service = new AuthenticationService(new Context(), new Tokens(), store, TimeProvider.System, Create(store));
        var result = await service.RefreshAsync();
        Assert.IsTrue(store.RotationAttempted);
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual("Authentication.RefreshRejected", result.Errors.OfType<BusinessError>().Single().Code);
        Assert.AreEqual("refresh", store.Session.RefreshTokenId);
    }

    [TestMethod]
    public async Task SessionIssuancePersistsTheSameIdentityAndTokenPair()
    {
        var store = new Store();
        var before = DateTime.UtcNow.AddMinutes(60);
        var pair = await Create(store).IssueAsync("user");
        Assert.AreEqual("user", store.Session.UserId);
        Assert.AreEqual(pair.RefreshToken, store.Session.RefreshTokenId);
        Assert.IsTrue(store.Session.ExpiresUtc >= before);
        Assert.IsTrue((await Create(store).ValidateAsync(store.Session.SessionId, "user", pair.RefreshToken)).IsSuccess);
    }

    private static SessionService Create(Store store) => new(store, new Tokens(), TimeProvider.System,
        Options.Create(new SessionOptions { RefreshExpiryInMinutes = 60 }));
    private sealed class Tokens : ITokenIssuer
    {
        public TokenPair Create(UserIdentity user, string sessionId, string refreshTokenId, DateTime refreshExpiration) =>
            new() { AccessToken = sessionId, RefreshToken = refreshTokenId };
    }
    private sealed class Context : IAuthenticationSession
    {
        public bool IsAuthenticated => true;
        public UserIdentity User => new() { UserId = "user" };
        public DateTime TokenExpirationTime => DateTime.UtcNow.AddMinutes(1);
        public string SessionId => "session";
        public string TokenId => "refresh";
    }
    private sealed class Store : IAuthenticationStore
    {
        public SessionState Session = new() { SessionId = "session", UserId = "user", RefreshTokenId = "refresh", ExpiresUtc = DateTime.UtcNow.AddHours(1) };
        public bool RejectRotation, RotationAttempted;
        public Task SetUserAsync(UserIdentity user, bool enabled, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserIdentity> GetUserAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(new UserIdentity { UserId = userId });
        public Task CreateSessionAsync(SessionState session, CancellationToken cancellationToken = default) { Session = session; return Task.CompletedTask; }
        public Task<ActiveSession> GetActiveSessionAsync(string sessionId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(Session != null && Session.SessionId == sessionId && Session.ExpiresUtc > nowUtc
                ? new ActiveSession { Session = Session, User = new UserIdentity { UserId = Session.UserId } } : null);
        public Task<bool> RotateAsync(string sessionId, string expectedTokenId, string nextTokenId, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            RotationAttempted = true;
            if (RejectRotation || Session.RefreshTokenId != expectedTokenId) return Task.FromResult(false);
            Session.RefreshTokenId = nextTokenId;
            return Task.FromResult(true);
        }
        public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default) { Session = null; return Task.CompletedTask; }
    }
}
