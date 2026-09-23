using My.XXX.Services.Authentication.Models;
using FluentResults;
using My.XXX.Contracts.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authentication.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthenticationSession>> RefreshAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

public interface ITokenIssuer
{
    TokenPair Create(UserIdentity user, string sessionId, string refreshTokenId, System.DateTime refreshExpiration);
}
