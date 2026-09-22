using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization.Interfaces;

/// <summary>按由应用自身持有的不透明缓存键存储权限投影。</summary>
public interface IPermissionCache
{
    Task<List<string>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task SetAsync(string userId, List<string> paths, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, CancellationToken cancellationToken = default);
}
