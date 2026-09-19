using FluentResults;
using My.XXX.Service.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthenticationSession>> Login(string ticket);
    Result<AuthenticationSession> Refresh();
    void Logout();
}

public interface ITokenIssuer
{
    TokenPair Issue(UserInfo user);
    TokenPair Refresh(UserInfo user, System.DateTime refreshExpiration);
}
