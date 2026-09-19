using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces;

public interface IPermissionCache
{
    Task<List<string>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task SetAsync(string userId, List<string> paths, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, CancellationToken cancellationToken = default);
}
