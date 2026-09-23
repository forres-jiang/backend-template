using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization.Ports;

public interface IPermissionStore
{
    List<string> GetPermissionCodes(List<Guid> roleIds);
    Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetPermissionCodesAsync(List<Guid> roleIds, CancellationToken cancellationToken = default);
}
