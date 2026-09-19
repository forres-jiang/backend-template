using FluentResults;
using System.Threading;
using System.Threading.Tasks;
using My.XXX.Service.DTOs;


namespace My.XXX.Service.Interfaces;

public interface IAuthenticationService
{
    Result<AuthenticationSession> Refresh();
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

public interface ITokenIssuer
{
    TokenPair Issue(UserInfo user);
    TokenPair Refresh(UserInfo user, System.DateTime refreshExpiration);
}
