using My.XXX.Service.Interfaces;
using System;
using System.Collections.Generic;

namespace My.XXX.Infrastructure;

public sealed class PermissionCache : IPermissionCache
{
    public bool TryGet(string userId, out List<string> paths)
    {
        paths = RedisHelper.Get<List<string>>(userId);
        return paths is { Count: > 0 };
    }

    public void Set(string userId, List<string> paths, TimeSpan expiration) =>
        RedisHelper.Set(userId, paths, expiration, null);

    public void Remove(string userId) => RedisHelper.Del(userId);
}
