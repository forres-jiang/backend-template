using Microsoft.Extensions.Options;
using My.XXX.Persistence.Interfaces;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service;

public sealed class PermissionQuery : IPermissionQuery, IScopeDependency
{
    private readonly IMenuRepository _menuRepository;
    private readonly IPermissionCache _permissionCache;
    private readonly AppConfig _appConfig;
    private readonly JwtConfig _jwtConfig;
    public PermissionQuery(IMenuRepository menus, IPermissionCache cache, IOptionsMonitor<AppConfig> app,
        IOptionsMonitor<JwtConfig> jwt)
    {
        _menuRepository = menus; _permissionCache = cache; _appConfig = app.CurrentValue; _jwtConfig = jwt.CurrentValue;
    }
    public List<string> GetRoleMenuPaths(List<Guid> roleIds)
    {
        var roleMenus = _menuRepository.GetRoleMenuByRoles(roleIds).ToList();
        var list = new List<string>();
        roleMenus.ForEach(m =>
        {
            if (!string.IsNullOrEmpty(m.ControllerName) && !string.IsNullOrEmpty(m.ActionName))
            {
                list.Add(m.ControllerName + "/" + m.ActionName);
            }
            else
            {
                if (!string.IsNullOrEmpty(m.Url))
                {
                    list.Add(m.Url);
                }
            }
        });
        return list;
    }

    public List<string> GetRoleMenuPaths(List<Guid> roleIds, string userId)
    {
        if (_appConfig.PermissionDataCache == PermissionDataCache.Redis)
        {
            if (_permissionCache.TryGet(userId, out var list))
            {
                return list;
            }

            var paths = GetRoleMenuPaths(roleIds);
            _permissionCache.Set(userId, paths, TimeSpan.FromMinutes(_jwtConfig.ExpiryInMinutes));
            return paths;
        }
        else
        {
            return GetRoleMenuPaths(roleIds);
        }
    }

}
