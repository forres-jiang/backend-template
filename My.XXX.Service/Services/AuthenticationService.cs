using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service;

public sealed class AuthenticationService(IAuthenticationSession current, ITokenIssuer tokens,
    IAuthenticationStore store, TimeProvider clock) : IAuthenticationService
{
    public async Task<Result<AuthenticationSession>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!current.IsAuthenticated || string.IsNullOrEmpty(current.SessionId) || string.IsNullOrEmpty(current.TokenId))
            return Result.Fail<AuthenticationSession>("Token invalid.");
        var active = await store.GetActiveSessionAsync(current.SessionId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (active == null || active.User.UserId != current.User?.UserId)
            return Result.Fail<AuthenticationSession>("Token invalid.");
        var nextId = Guid.NewGuid().ToString("N");
        var pair = tokens.Create(active.User, current.SessionId, nextId, active.Session.ExpiresUtc);
        if (!await store.RotateAsync(current.SessionId, current.TokenId, nextId, clock.GetUtcNow().UtcDateTime, cancellationToken))
            return Result.Fail<AuthenticationSession>("Refresh token has already been used or revoked.");
        return Result.Ok(new AuthenticationSession { Tokens = pair });
    }
    public Task LogoutAsync(CancellationToken cancellationToken = default) => string.IsNullOrEmpty(current.SessionId)
        ? Task.CompletedTask : store.RevokeAsync(current.SessionId, cancellationToken);
}
