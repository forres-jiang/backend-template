using My.XXX.Services.Authorization.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Infrastructure;

// Used only when Redis is not configured and permissions come from the database.
public sealed class NullPermissionCache : IPermissionCache
{
    public Task<List<string>> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<List<string>>(null);

    public Task SetAsync(string userId, List<string> paths, TimeSpan expiration, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
