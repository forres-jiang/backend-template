using FluentResults;
using My.XXX.Service.DTOs;
using System.Threading;
using System.Threading.Tasks;


namespace My.XXX.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthenticationSession>> RefreshAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

public interface ITokenIssuer
{
    Task<TokenPair> IssueAsync(string userId, CancellationToken cancellationToken = default);
    TokenPair Create(UserInfo user, string sessionId, string refreshTokenId, System.DateTime refreshExpiration);
}
