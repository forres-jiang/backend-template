using FluentResults;
using Microsoft.Extensions.Options;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Service;

public sealed class AuthenticationService : IAuthenticationService, IScopeDependency
{
    private readonly ICurrentRequest _current;
    private readonly ITokenIssuer _tokens;
    private readonly IPermissionQuery _permissions;
    private readonly AppConfig _config;

    public AuthenticationService(ICurrentRequest current, ITokenIssuer tokens,
        IPermissionQuery permissions, IOptionsMonitor<AppConfig> config)
    {
        _current = current;
        _tokens = tokens; _permissions = permissions; _config = config.CurrentValue;
    }

    public Result<AuthenticationSession> Refresh()
    {
        if (_current.Principal?.Identity?.IsAuthenticated != true || _current.User == null)
            return Result.Fail<AuthenticationSession>("Token invalid.");
        return Result.Ok(new AuthenticationSession { Tokens = _tokens.Refresh(_current.User, _current.TokenExpirationTime) });
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (_config.PermissionDataCache == PermissionDataCache.Redis && _current.User != null)
            await _permissions.RemoveCachedPermissionsAsync(_current.User.RoleIds, _current.User.UserId, cancellationToken);
    }
}