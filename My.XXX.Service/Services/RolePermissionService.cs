using FluentResults;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Service;

public sealed class RolePermissionService(MenuMutations mutations, ICurrentUser current, TimeProvider clock)
{
    private Result Change(Guid roleId, List<int> menuIds, RoleMenuChange change)
    {
        if (roleId == Guid.Empty || menuIds == null || menuIds.Any(id => id <= 0))
            return Result.Fail(MenuErrors.InvalidSelection());
        return mutations.SetRoleMenus(roleId, menuIds, change, current.User?.UserId);
    }
    public Result RoleMenus(Guid roleId, List<int> ids, bool isFull) =>
        Change(roleId, ids, isFull ? RoleMenuChange.Replace : RoleMenuChange.Add);
    public Result RoleMenuRelation(InputRoleMenu input) => input?.Checked == null ? Result.Fail(MenuErrors.InvalidSelection())
        : Change(input.RoleId, new() { input.MenuId }, input.Checked.Value ? RoleMenuChange.Add : RoleMenuChange.Remove);
    public Result RoleMenusRelation(InputRoleMenus input) => input?.Checked == null ? Result.Fail(MenuErrors.InvalidSelection())
        : Change(input.RoleId, input.MenuIds, input.Checked.Value ? RoleMenuChange.Add : RoleMenuChange.Remove);
    public Result RemoveRoleMenu(Guid roleId, int menuId) => Change(roleId, new() { menuId }, RoleMenuChange.Remove);
    public Result<BatchWriteSummary> RoleMenuAction(RoleMenuActionModel model)
    {
        if (model == null || model.RoleId == Guid.Empty || model.Menus == null || model.Menus.Count == 0 ||
            model.Menus.Any(m => m == null || m.MenuId <= 0 || (m.ActionIds != null && m.ActionIds.Any(id => id <= 0))))
            return Result.Fail<BatchWriteSummary>(MenuErrors.InvalidSelection());
        var ids = RoleMenuRelations.Build(model);
        var started = clock.GetLocalNow().DateTime;
        var result = mutations.SetRoleMenus(model.RoleId, ids, RoleMenuChange.Replace, current.User?.UserId);
        return result.IsSuccess ? Result.Ok(new BatchWriteSummary { RowsCopied = ids.Count, StartTime = started }) : Result.Fail<BatchWriteSummary>(result.Errors);
    }
}
