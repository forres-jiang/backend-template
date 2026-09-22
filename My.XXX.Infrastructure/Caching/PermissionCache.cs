using My.XXX.Services.Authorization.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Infrastructure;

public sealed class PermissionCache(IConnectionMultiplexer connection) : IPermissionCache
{
    public async Task<List<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await connection.GetDatabase().StringGetAsync(userId).WaitAsync(cancellationToken);
        // 在迁移期间保留现有的 user-id 键和 JSON 数组。
        return value.IsNull ? null : JsonConvert.DeserializeObject<List<string>>((string)value);
    }

    public async Task SetAsync(string userId, List<string> paths, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(paths);
        if (expiration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(expiration));
        cancellationToken.ThrowIfCancellationRequested();
        // SET 原子地包含 TTL；绝不能无限期缓存权限。
        await connection.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(paths), expiration)
            .WaitAsync(cancellationToken);
    }

    public async Task RemoveAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        cancellationToken.ThrowIfCancellationRequested();
        await connection.GetDatabase().KeyDeleteAsync(userId).WaitAsync(cancellationToken);
    }
}
