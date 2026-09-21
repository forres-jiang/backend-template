using FluentResults;
using My.XXX.Services.Common;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Services;
public sealed class PermissionAdministration(IRolePermissionStore store) : IPermissionAdministration
{
    public Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default) => store.GetAsync(roleId, cancellationToken);
    public async Task<Result> ReplaceAsync(Guid roleId, List<string> codes, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty || codes == null || codes.Any(code => !PermissionCodes.All.Contains(code)))
            return Result.Fail(new BusinessError("Unknown permission code or invalid role.", code: "Permission.InvalidInput"));
        await store.ReplaceAsync(roleId, codes.Distinct().ToList(), cancellationToken);
        return Result.Ok();
    }
}
