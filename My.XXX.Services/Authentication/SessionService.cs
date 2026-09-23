using FluentResults;
using Microsoft.Extensions.Options;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Authentication.Interfaces;
using My.XXX.Services.Authentication.Models;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace My.XXX.Services.Authentication;

public sealed class SessionService(IAuthenticationStore store, ITokenIssuer tokens,
    TimeProvider clock, IOptions<SessionOptions> options) : ISessionService
{
    private static readonly Meter Meter = new("My.XXX.Authentication");
    private static readonly Histogram<double> ValidationDuration = Meter.CreateHistogram<double>("authentication.validation.duration", "ms");
    public async Task<Result<ActiveSession>> ValidateAsync(string sessionId, string userId,
        string refreshTokenId = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        try { return await ValidateCoreAsync(sessionId, userId, refreshTokenId, cancellationToken); }
        finally { ValidationDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    }

    private async Task<Result<ActiveSession>> ValidateCoreAsync(string sessionId, string userId,
        string refreshTokenId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
            return Result.Fail<ActiveSession>(AuthenticationErrors.InvalidToken());
        var active = await store.GetActiveSessionAsync(sessionId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (active == null || active.User.UserId != userId ||
            (refreshTokenId != null && active.Session.RefreshTokenId != refreshTokenId))
            return Result.Fail<ActiveSession>(AuthenticationErrors.InvalidToken());
        return Result.Ok(active);
    }

    public async Task<TokenPair> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("The user is not enabled in the identity authority.");
        if (options.Value.RefreshExpiryInMinutes <= 0)
            throw new InvalidOperationException("Configure a positive session lifetime.");
        var session = new SessionState
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            RefreshTokenId = Guid.NewGuid().ToString("N"),
            ExpiresUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(options.Value.RefreshExpiryInMinutes)
        };
        var pair = tokens.Create(user, session.SessionId, session.RefreshTokenId, session.ExpiresUtc);
        await store.CreateSessionAsync(session, cancellationToken);
        return pair;
    }
}

public static class AuthenticationErrors
{
    public static BusinessError InvalidToken() => new("Token invalid.", code: "Authentication.InvalidToken", kind: BusinessErrorKind.Unauthorized);
    public static BusinessError RefreshRejected() => new("Refresh token has already been used or revoked.", code: "Authentication.RefreshRejected", kind: BusinessErrorKind.Unauthorized);
}
