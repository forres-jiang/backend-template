using FluentResults;
using Microsoft.Extensions.Options;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.Service;

public sealed class AuthenticationService : IAuthenticationService, IScopeDependency
{
    private readonly IAppCenterService _users;
    private readonly ICurrentRequest _current;
    private readonly ITokenIssuer _tokens;
    private readonly IPermissionCache _cache;
    private readonly AppConfig _config;

    public AuthenticationService(IAppCenterService users, ICurrentRequest current, ITokenIssuer tokens,
        IPermissionCache cache, IOptionsMonitor<AppConfig> config)
    {
        _users = users; _current = current; _tokens = tokens; _cache = cache; _config = config.CurrentValue;
    }

    public async Task<Result<AuthenticationSession>> Login(string ticket)
    {
        if (string.IsNullOrEmpty(ticket)) return Result.Fail<AuthenticationSession>("Ticket invalid.");
        var user = await _users.GetUserByTicket(ticket);
        if (user == null) return Result.Fail<AuthenticationSession>("Ticket invalid.");
        var roles = user.Roles ?? new List<ApplicationRole>();
        var info = new UserInfo
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.EmailAddress,
            Roles = roles.Select(role => role.RoleName).Distinct().ToList(),
            RoleIds = roles.Select(role => role.RoleID).Distinct().ToList(),
            Menus = new List<MenuDto>()
        };
        return Result.Ok(new AuthenticationSession
        {
            User = new LoginUser { UserId = info.UserId, UserName = info.UserName, Roles = info.Roles, Menus = info.Menus },
            Tokens = _tokens.Issue(info)
        });
    }

    public Result<AuthenticationSession> Refresh()
    {
        if (_current.Principal?.Identity?.IsAuthenticated != true || _current.User == null)
            return Result.Fail<AuthenticationSession>("Token invalid.");
        return Result.Ok(new AuthenticationSession { Tokens = _tokens.Refresh(_current.User, _current.TokenExpirationTime) });
    }

    public void Logout()
    {
        if (_config.PermissionDataCache == PermissionDataCache.Redis && _current.User != null)
            _cache.Remove(_current.User.UserId);
    }
}
