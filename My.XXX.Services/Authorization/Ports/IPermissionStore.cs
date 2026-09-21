using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization.Ports;

public interface IPermissionStore
{
    List<string> GetPermissionPaths(List<Guid> roleIds);
    Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetPermissionPathsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default);
}
