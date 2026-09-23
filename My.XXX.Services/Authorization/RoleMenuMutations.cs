using FluentResults;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Policies;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization;

/// <summary>在共享的加锁事务内执行的角色/菜单分配规则。</summary>
public sealed class RoleMenuMutations(TimeProvider clock)
{
    /// <summary>Compose rules without opening or committing another transaction.</summary>
    public async Task<Result> SetRoleMenus(IAccessControlWriteSession session, Guid roleId, List<int> ids,
        RoleMenuChange change, string userId, CancellationToken cancellationToken = default)
    {
        if (!ValidSelection(roleId, ids, change))
            return Result.Fail(RoleMenuErrors.InvalidSelection());
        var selected = ids.Distinct().ToList();
        if (change != RoleMenuChange.Remove && selected.Except(await session.LoadMenuIds(cancellationToken)).Any())
            return Result.Fail(RoleMenuErrors.InvalidSelection());
        var existing = await session.LoadRoleMenus(roleId, cancellationToken);
        var removals = change == RoleMenuChange.Replace ? existing.Except(selected).ToList()
            : change == RoleMenuChange.Remove ? existing.Intersect(selected).ToList() : new();
        var additions = change == RoleMenuChange.Remove ? new List<int>() : selected.Except(existing).ToList();
        return Result.OkIf(await session.ApplyRoleChanges(roleId, additions, removals, userId, clock.GetLocalNow().DateTime, cancellationToken), RoleMenuErrors.WriteFailed());
    }

    private static bool ValidSelection(Guid roleId, List<int> ids, RoleMenuChange change) =>
        roleId != Guid.Empty && ids != null && ids.All(id => id > 0) && Enum.IsDefined(change);
}
