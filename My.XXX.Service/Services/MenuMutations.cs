using FluentResults;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Models;
using My.XXX.Service.Ports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service;

/// <summary>Owns menu write rules. All state-dependent decisions run inside the adapter's locked transaction.</summary>
public sealed class MenuMutations(IMenuTransaction transaction, TimeProvider clock)
{
    public Result Add(SaveMenu input, string culture, string userId)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.DisplayName) || !input.IsAction.HasValue || input.ParentId < 0 || input.Number < 0)
            return Result.Fail(MenuErrors.InvalidInput());
        return transaction.Execute(session =>
        {
            if (!MenuHierarchy.CanPlace(session.LoadMenus(), 0, input.ParentId, input.IsAction.Value))
                return Result.Fail(MenuErrors.InvalidParent());
            var now = clock.GetLocalNow().DateTime;
            var menu = new MenuState
            {
                DisplayName = input.DisplayName.Trim(),
                DisplayNames = MenuDisplayNames.Set(null, culture, input.DisplayName),
                Description = input.Description,
                Icon = input.Icon,
                Url = input.Url,
                Component = input.Component,
                ControllerName = input.ControllerName?.Trim(),
                ActionName = input.ActionName?.Trim(),
                LinkTarget = input.LinkTarget,
                Number = input.Number,
                ParentId = input.ParentId,
                IsDisplay = input.IsDisplay,
                IsAction = input.IsAction.Value,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedTime = now,
                UpdatedTime = now
            };
            return Result.OkIf(session.Insert(menu) == 1, MenuErrors.WriteFailed());
        });
    }

    public Result Update(EditMenu input, string culture, string userId)
    {
        if (input == null || input.Id <= 0 || input.ParentId < 0 || input.Number < 0 || !MenuUpdateFields.Valid(input.ClearFields))
            return Result.Fail(MenuErrors.InvalidInput());
        return transaction.Execute(session =>
        {
            var menus = session.LoadMenus();
            var menu = menus.FirstOrDefault(m => m.Id == input.Id)?.Copy();
            if (menu == null) return Result.Fail(MenuErrors.NotFound());
            menu.ParentId = input.ParentId ?? menu.ParentId;
            menu.IsAction = input.IsAction ?? menu.IsAction;
            if (!MenuHierarchy.CanPlace(menus, menu.Id, menu.ParentId, menu.IsAction)) return Result.Fail(MenuErrors.InvalidParent());
            var clear = new HashSet<string>(input.ClearFields ?? new(), StringComparer.OrdinalIgnoreCase);
            string Value(string field, string value, string old) => clear.Contains(field) ? null : value?.Trim() ?? old;
            menu.Description = Value(nameof(input.Description), input.Description, menu.Description);
            menu.Icon = Value(nameof(input.Icon), input.Icon, menu.Icon);
            menu.Url = Value(nameof(input.Url), input.Url, menu.Url);
            menu.Component = Value(nameof(input.Component), input.Component, menu.Component);
            menu.ControllerName = Value(nameof(input.ControllerName), input.ControllerName, menu.ControllerName);
            menu.ActionName = Value(nameof(input.ActionName), input.ActionName, menu.ActionName);
            menu.LinkTarget = Value(nameof(input.LinkTarget), input.LinkTarget, menu.LinkTarget);
            if (!string.IsNullOrWhiteSpace(input.DisplayName))
            {
                menu.DisplayName = input.DisplayName.Trim();
                menu.DisplayNames = MenuDisplayNames.Set(menu.DisplayNames, culture, input.DisplayName);
            }
            menu.Number = input.Number ?? menu.Number;
            menu.IsDisplay = input.IsDisplay ?? menu.IsDisplay;
            Stamp(menu, userId);
            return Result.OkIf(session.Update(menu) == 1, MenuErrors.WriteFailed());
        });
    }

    public Result Remove(List<int> ids, string userId)
    {
        if (ids == null || ids.Count == 0 || ids.Any(id => id <= 0)) return Result.Fail(MenuErrors.InvalidInput());
        return transaction.Execute(session => Result.OkIf(session.Remove(ids.Distinct().ToList(), userId, clock.GetLocalNow().DateTime) > 0, MenuErrors.NotFound()));
    }

    public Result Move(MenuSortModel input, string userId) => transaction.Execute(session =>
    {
        var menus = session.LoadMenus();
        var order = MenuOrder.Build(menus, input);
        if (order == null) return Result.Fail(MenuErrors.InvalidOrder());
        var current = order.First(m => m.Id == input.CurrentId);
        if (!MenuHierarchy.CanPlace(menus, current.Id, current.ParentId, current.IsAction)) return Result.Fail(MenuErrors.InvalidParent());
        for (var index = 0; index < order.Count; index++)
        {
            var menu = order[index];
            menu.Number = index;
            Stamp(menu, userId);
            if (session.Update(menu) != 1) return Result.Fail(MenuErrors.WriteFailed());
        }
        return Result.Ok();
    });

    public Result SetRoleMenus(Guid roleId, List<int> ids, RoleMenuChange change, string userId)
    {
        if (roleId == Guid.Empty || ids == null || ids.Any(id => id <= 0) || !Enum.IsDefined(change))
            return Result.Fail(MenuErrors.InvalidSelection());
        var selected = ids.Distinct().ToList();
        return transaction.Execute(session =>
        {
            if (change != RoleMenuChange.Remove && selected.Except(session.LoadMenus().Select(m => m.Id)).Any())
                return Result.Fail(MenuErrors.InvalidSelection());
            var existing = session.LoadRoleMenus(roleId);
            var removals = change == RoleMenuChange.Replace ? existing.Except(selected).ToList()
                : change == RoleMenuChange.Remove ? existing.Intersect(selected).ToList() : new();
            var additions = change == RoleMenuChange.Remove ? new List<int>() : selected.Except(existing).ToList();
            return Result.OkIf(session.ApplyRoleChanges(roleId, additions, removals, userId, clock.GetLocalNow().DateTime), MenuErrors.WriteFailed());
        });
    }

    private void Stamp(MenuState menu, string userId)
    {
        menu.UpdatedBy = userId;
        menu.UpdatedTime = clock.GetLocalNow().DateTime;
    }
}
