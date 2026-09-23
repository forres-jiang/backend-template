using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization;

public sealed class PermissionQuery(IPermissionStore store) : IPermissionQuery
{
    public List<string> GetPermissionCodes(List<Guid> roleIds) => store.GetPermissionCodes(roleIds);
    public Task<List<string>> GetPermissionCodesAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        return store.GetPermissionCodesAsync(roleIds, cancellationToken);
    }
    public Task RemoveCachedPermissionsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
