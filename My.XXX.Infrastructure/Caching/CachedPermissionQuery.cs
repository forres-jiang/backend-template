using Microsoft.Extensions.Options;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace My.XXX.Infrastructure.Caching;

public sealed class CachedPermissionQuery(IPermissionStore menus, IPermissionCache cache,
    IOptions<PermissionCacheOptions> options) : IPermissionQuery
{
    private static readonly Meter Meter = new("My.XXX.Authorization");
    private static readonly Counter<long> Hits = Meter.CreateCounter<long>("permissions.cache.hits");
    private static readonly Counter<long> Misses = Meter.CreateCounter<long>("permissions.cache.misses");
    private static readonly Counter<long> Retries = Meter.CreateCounter<long>("permissions.revision.retries");
    private static readonly Counter<long> Fallbacks = Meter.CreateCounter<long>("permissions.database.fallbacks");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("permissions.query.duration", "ms");

    public async Task<List<string>> GetPermissionCodesAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        try { return await QueryAsync(roleIds, userId, cancellationToken); }
        finally { Duration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    }

    private async Task<List<string>> QueryAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        var config = options.Value;
        if (config.ExpiryInMinutes <= 0 || string.IsNullOrWhiteSpace(config.KeyPrefix))
            throw new InvalidOperationException("Permission cache requires a positive expiry and a key prefix.");
        // 在缓存/数据库读取之后重新检查。绝不能以不属于某次修订（revision）的版本发布数据。
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var revision = await menus.GetPermissionRevisionAsync(cancellationToken);
            var key = PermissionCacheKey.Create(config.KeyPrefix, userId, roleIds, revision);
            var cached = await cache.GetAsync(key, cancellationToken);
            if (cached == null) Misses.Add(1); else Hits.Add(1);
            var paths = cached ?? await menus.GetPermissionCodesAsync(roleIds, cancellationToken);
            if (revision != await menus.GetPermissionRevisionAsync(cancellationToken)) { Retries.Add(1); continue; }
            if (cached == null) await cache.SetAsync(key, paths, TimeSpan.FromMinutes(config.ExpiryInMinutes), cancellationToken);
            return paths;
        }
        // 持续编辑的情况下：直接读取当前权限，而不是返回过期的缓存授权。
        Fallbacks.Add(1);
        return await menus.GetPermissionCodesAsync(roleIds, cancellationToken);
    }
}
