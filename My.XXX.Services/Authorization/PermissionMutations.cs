using FluentResults;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Policies;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization;

/// <summary>Permission rules composed inside an existing access-control transaction.</summary>
public sealed class PermissionMutations
{
    public async Task<Result> ReplaceAsync(IAccessControlWriteSession session, Guid roleId,
        List<string> codes, CancellationToken cancellationToken = default)
    {
        var validation = Validate(roleId, codes);
        if (validation.IsFailed) return validation;
        await session.ReplaceRolePermissions(roleId, codes.Distinct().ToList(), cancellationToken);
        return Result.Ok();
    }

    internal static Result Validate(Guid roleId, List<string> codes) =>
        roleId == Guid.Empty || codes == null || codes.Any(code => !PermissionCodes.All.Contains(code))
            ? Result.Fail(new BusinessError("Unknown permission code or invalid role.", code: "Permission.InvalidInput"))
            : Result.Ok();
}
