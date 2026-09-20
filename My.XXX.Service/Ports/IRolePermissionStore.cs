using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service.Ports;
public interface IRolePermissionStore
{
    Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task ReplaceAsync(Guid roleId, List<string> codes, CancellationToken cancellationToken = default);
}
