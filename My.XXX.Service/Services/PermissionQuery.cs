using Microsoft.Extensions.Options;
using My.XXX.Service.Ports;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Common;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service;

public sealed class PermissionQuery(IMenuRepository menus, IPermissionCache cache,
    IOptionsMonitor<AppConfig> app, IOptions<PermissionCacheOptions> options) : IPermissionQuery, IScopeDependency
{
    public List<string> GetRoleMenuPaths(List<Guid> roleIds) => menus.GetRoleMenuByRoles(roleIds)
        .Select(m => !string.IsNullOrEmpty(m.ControllerName) && !string.IsNullOrEmpty(m.ActionName)
            ? m.ControllerName + "/" + m.ActionName : m.Url).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();

    public async Task<List<string>> GetRoleMenuPathsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        if (app.CurrentValue.PermissionDataCache != PermissionDataCache.Redis)
            return await menus.GetPermissionPathsAsync(roleIds, cancellationToken);
        var config = options.Value;
        if (config.ExpiryInMinutes <= 0 || string.IsNullOrWhiteSpace(config.KeyPrefix))
            throw new InvalidOperationException("Permission cache requires a positive expiry and a key prefix.");
        // Recheck after the cache/database read. Never publish data under a revision it did not belong to.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var revision = await menus.GetPermissionRevisionAsync(cancellationToken);
            var key = PermissionCacheKey.Create(config.KeyPrefix, userId, roleIds, revision);
            var cached = await cache.GetAsync(key, cancellationToken);
            var paths = cached ?? await menus.GetPermissionPathsAsync(roleIds, cancellationToken);
            if (revision != await menus.GetPermissionRevisionAsync(cancellationToken)) continue;
            if (cached == null) await cache.SetAsync(key, paths, TimeSpan.FromMinutes(config.ExpiryInMinutes), cancellationToken);
            return paths;
        }
        // Continuous edits: read current permissions directly instead of returning an old cached grant.
        return await menus.GetPermissionPathsAsync(roleIds, cancellationToken);
    }
    public async Task RemoveCachedPermissionsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        if (app.CurrentValue.PermissionDataCache != PermissionDataCache.Redis) return;
        var revision = await menus.GetPermissionRevisionAsync(cancellationToken);
        await cache.RemoveAsync(PermissionCacheKey.Create(options.Value.KeyPrefix, userId, roleIds, revision), cancellationToken);
        // Also clear the former key during rolling data-format migration.
        await cache.RemoveAsync(userId, cancellationToken);
    }
}
