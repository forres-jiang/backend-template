using My.XXX.Service.DTOs;
using System;
using System.Security.Claims;

namespace My.XXX.Service.Interfaces;

/// <summary>Framework-neutral view of the current request and authenticated user.</summary>
public interface ICurrentRequest
{
    ClaimsPrincipal Principal { get; }
    UserInfo User { get; }
    DateTime TokenExpirationTime { get; }
    string CultureName { get; }
}
