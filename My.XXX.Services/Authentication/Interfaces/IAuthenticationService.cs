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
    TokenPair Create(UserInfo user, string sessionId, string refreshTokenId, System.DateTime refreshExpiration);
}
