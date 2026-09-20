using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Service;

public sealed class PermissionQuery(IPermissionStore store) : IPermissionQuery
{
    public List<string> GetRoleMenuPaths(List<Guid> roleIds) => store.GetPermissionPaths(roleIds);
    public Task<List<string>> GetRoleMenuPathsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        return store.GetPermissionPathsAsync(roleIds, cancellationToken);
    }
    public Task RemoveCachedPermissionsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
