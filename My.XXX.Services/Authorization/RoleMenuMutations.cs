using FluentResults;
using My.XXX.Services.Authorization.Ports;
using My.XXX.Services.Menus.Policies;
using My.XXX.Services.Menus.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization;

/// <summary>Role/menu assignment rules executed inside the shared locked transaction.</summary>
public sealed class RoleMenuMutations(IMenuTransaction transaction, TimeProvider clock)
{
    public async Task<Result> SetRoleMenus(Guid roleId, List<int> ids, RoleMenuChange change, string userId, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty || ids == null || ids.Any(id => id <= 0) || !Enum.IsDefined(change))
            return Result.Fail(MenuErrors.InvalidSelection());
        var selected = ids.Distinct().ToList();
        return (await transaction.Execute(async session =>
        {
            if (change != RoleMenuChange.Remove && selected.Except((await session.LoadMenus(cancellationToken)).Select(m => m.Id)).Any())
                return Result.Fail(MenuErrors.InvalidSelection());
            var existing = (await session.LoadRoleMenus(roleId, cancellationToken));
            var removals = change == RoleMenuChange.Replace ? existing.Except(selected).ToList()
                : change == RoleMenuChange.Remove ? existing.Intersect(selected).ToList() : new();
            var additions = change == RoleMenuChange.Remove ? new List<int>() : selected.Except(existing).ToList();
            return Result.OkIf((await session.ApplyRoleChanges(roleId, additions, removals, userId, clock.GetLocalNow().DateTime, cancellationToken)), MenuErrors.WriteFailed());
        }, cancellationToken));
    }

}
