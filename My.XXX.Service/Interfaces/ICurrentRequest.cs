using System.Security.Claims;

namespace My.XXX.Service.Interfaces;

/// <summary>Framework-neutral view of the current request and authenticated user.</summary>
public interface ICurrentRequest : IAuthenticationSession, ICurrentCulture
{
    ClaimsPrincipal Principal { get; }
    bool IAuthenticationSession.IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
