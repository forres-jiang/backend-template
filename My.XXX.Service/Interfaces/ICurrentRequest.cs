using System.Security.Claims;

namespace My.XXX.Service.Interfaces;

/// <summary>Framework-neutral view of the current request and authenticated user.</summary>
public interface ICurrentRequest : IAuthenticationSession, ICurrentCulture
{
    ClaimsPrincipal Principal { get; }
    bool IAuthenticationSession.IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    string IAuthenticationSession.SessionId => Principal?.FindFirst("sid")?.Value;
    string IAuthenticationSession.TokenId => Principal?.FindFirst("jti")?.Value;
}
