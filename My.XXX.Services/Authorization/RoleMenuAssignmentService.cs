using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Policies;
using My.XXX.Services.Authorization.Ports;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Services.Authorization;

public sealed class RoleMenuAssignmentService(IAccessControlTransaction transaction, RoleMenuMutations mutations,
    ICurrentUser current, TimeProvider clock)
{
    public async Task<Result> SetRoleMenus(Guid roleId, List<int> menuIds, RoleMenuChange change, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty || menuIds == null || menuIds.Any(id => id <= 0))
            return Result.Fail(RoleMenuErrors.InvalidSelection());
        return (await transaction.Execute(session => mutations.SetRoleMenus(session, roleId, menuIds, change, current.User?.UserId, cancellationToken), cancellationToken));
    }
    public async Task<Result> RoleMenus(Guid roleId, List<int> ids, bool isFull, CancellationToken cancellationToken = default) =>
        (await SetRoleMenus(roleId, ids, isFull ? RoleMenuChange.Replace : RoleMenuChange.Add, cancellationToken));
    public async Task<Result> RoleMenuRelation(InputRoleMenu input, CancellationToken cancellationToken = default) => input?.Checked == null ? Result.Fail(RoleMenuErrors.InvalidSelection())
        : (await SetRoleMenus(input.RoleId, new() { input.MenuId }, input.Checked.Value ? RoleMenuChange.Add : RoleMenuChange.Remove, cancellationToken));
    public async Task<Result> RoleMenusRelation(InputRoleMenus input, CancellationToken cancellationToken = default) => input?.Checked == null ? Result.Fail(RoleMenuErrors.InvalidSelection())
        : (await SetRoleMenus(input.RoleId, input.MenuIds, input.Checked.Value ? RoleMenuChange.Add : RoleMenuChange.Remove, cancellationToken));
    public async Task<Result> RemoveRoleMenu(Guid roleId, int menuId, CancellationToken cancellationToken = default) => (await SetRoleMenus(roleId, new() { menuId }, RoleMenuChange.Remove, cancellationToken));
    public async Task<Result<BatchWriteSummary>> RoleMenuAction(RoleMenuActionModel model, CancellationToken cancellationToken = default)
    {
        if (model == null || model.RoleId == Guid.Empty || model.Menus == null || model.Menus.Count == 0 ||
            model.Menus.Any(m => m == null || m.MenuId <= 0 || (m.ActionIds != null && m.ActionIds.Any(id => id <= 0))))
            return Result.Fail<BatchWriteSummary>(RoleMenuErrors.InvalidSelection());
        var ids = RoleMenuRelations.Build(model);
        var started = clock.GetLocalNow().DateTime;
        var result = (await SetRoleMenus(model.RoleId, ids, RoleMenuChange.Replace, cancellationToken));
        return result.IsSuccess ? Result.Ok(new BatchWriteSummary { RowsCopied = ids.Count, StartTime = started }) : Result.Fail<BatchWriteSummary>(result.Errors);
    }
}
