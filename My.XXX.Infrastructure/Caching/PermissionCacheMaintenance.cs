using Microsoft.Extensions.Options;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Infrastructure.Caching;

/// <summary>Operational cache cleanup; authorization queries do not depend on this capability.</summary>
public sealed class PermissionCacheMaintenance(IPermissionStore menus, IPermissionCache cache,
    IOptions<PermissionCacheOptions> options)
{
    public async Task RemoveCachedPermissionsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        var revision = await menus.GetPermissionRevisionAsync(cancellationToken);
        await cache.RemoveAsync(PermissionCacheKey.Create(options.Value.KeyPrefix, userId, roleIds, revision), cancellationToken);
        // 在数据格式滚动迁移期间，同时清除旧键。
        await cache.RemoveAsync(userId, cancellationToken);
    }
}
