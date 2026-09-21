using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Interfaces;

public interface IPermissionQuery
{
    Task RemoveCachedPermissionsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default);
    List<string> GetRoleMenuPaths(List<Guid> roleIds);
    Task<List<string>> GetRoleMenuPathsAsync(List<Guid> roleIds, string userId, CancellationToken cancellationToken = default);
}
