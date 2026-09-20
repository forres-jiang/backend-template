using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Service;

public sealed class AuthenticationService : IAuthenticationService, IScopeDependency
{
    private readonly IAuthenticationSession _current;
    private readonly ITokenIssuer _tokens;
    private readonly IPermissionQuery _permissions;

    public AuthenticationService(IAuthenticationSession current, ITokenIssuer tokens,
        IPermissionQuery permissions)
    {
        _current = current;
        _tokens = tokens; _permissions = permissions;
    }

    public Result<AuthenticationSession> Refresh()
    {
        if (!_current.IsAuthenticated || _current.User == null)
            return Result.Fail<AuthenticationSession>("Token invalid.");
        return Result.Ok(new AuthenticationSession { Tokens = _tokens.Refresh(_current.User, _current.TokenExpirationTime) });
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (_current.User != null)
            await _permissions.RemoveCachedPermissionsAsync(_current.User.RoleIds, _current.User.UserId, cancellationToken);
    }
}
