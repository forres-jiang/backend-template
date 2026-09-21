using Microsoft.Extensions.Options;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Infrastructure.Caching;

public sealed class CachedPermissionQuery(IPermissionStore menus, IPermissionCache cache,
    IOptions<PermissionCacheOptions> options) : IPermissionQuery
{
    public List<string> GetRoleMenuPaths(List<Guid> roleIds) => menus.GetPermissionPaths(roleIds);

    public async Task<List<string>> GetRoleMenuPathsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
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
        var revision = await menus.GetPermissionRevisionAsync(cancellationToken);
        await cache.RemoveAsync(PermissionCacheKey.Create(options.Value.KeyPrefix, userId, roleIds, revision), cancellationToken);
        // Also clear the former key during rolling data-format migration.
        await cache.RemoveAsync(userId, cancellationToken);
    }
}
