using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Menus.Models;
using My.XXX.Services.Menus.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Menus;

/// <summary>负责菜单写入规则。所有依赖状态的决策都在适配器提供的加锁事务内运行。</summary>
public sealed class MenuMutations(IAccessControlTransaction transaction, TimeProvider clock)
{
    public async Task<Result> Add(SaveMenu input, string culture, string userId, CancellationToken cancellationToken = default)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.DisplayName) || !input.IsAction.HasValue || input.ParentId < 0 || input.Number < 0)
            return Result.Fail(MenuErrors.InvalidInput());
        return (await transaction.Execute(async session =>
        {
            if (!MenuHierarchy.CanPlace((await session.LoadMenus(cancellationToken)), 0, input.ParentId, input.IsAction.Value))
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
            return Result.OkIf((await session.Insert(menu, cancellationToken)) == 1, MenuErrors.WriteFailed());
        }, cancellationToken));
    }

    public async Task<Result> Update(EditMenu input, string culture, string userId, CancellationToken cancellationToken = default)
    {
        if (input == null || input.Id <= 0 || input.ParentId < 0 || input.Number < 0 || !MenuUpdateFields.Valid(input.ClearFields))
            return Result.Fail(MenuErrors.InvalidInput());
        return (await transaction.Execute(async session =>
        {
            var menus = (await session.LoadMenus(cancellationToken));
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
            return Result.OkIf((await session.Update(menu, cancellationToken)) == 1, MenuErrors.WriteFailed());
        }, cancellationToken));
    }

    public async Task<Result> Remove(List<int> ids, string userId, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0 || ids.Any(id => id <= 0)) return Result.Fail(MenuErrors.InvalidInput());
        return (await transaction.Execute(async session =>
        {
            var selected = ids.Distinct().ToList();
            var menus = (await session.LoadMenus(cancellationToken));
            if (selected.Except(menus.Select(m => m.Id)).Any()) return Result.Fail(MenuErrors.NotFound());
            if (menus.Any(m => selected.Contains(m.ParentId) && !selected.Contains(m.Id)))
                return Result.Fail(MenuErrors.HasChildren());
            return Result.OkIf((await session.Remove(selected, userId, clock.GetLocalNow().DateTime, cancellationToken)) == selected.Count, MenuErrors.WriteFailed());
        }, cancellationToken));
    }

    public async Task<Result> Move(MenuSortModel input, string userId, CancellationToken cancellationToken = default) => (await transaction.Execute(async session =>
    {
        var menus = (await session.LoadMenus(cancellationToken));
        var order = MenuOrder.Build(menus, input);
        if (order == null) return Result.Fail(MenuErrors.InvalidOrder());
        var current = order.First(m => m.Id == input.CurrentId);
        if (!MenuHierarchy.CanPlace(menus, current.Id, current.ParentId, current.IsAction)) return Result.Fail(MenuErrors.InvalidParent());
        for (var index = 0; index < order.Count; index++)
        {
            var menu = order[index];
            menu.Number = index;
            Stamp(menu, userId);
            if ((await session.Update(menu, cancellationToken)) != 1) return Result.Fail(MenuErrors.WriteFailed());
        }
        return Result.Ok();
    }, cancellationToken));

    private void Stamp(MenuState menu, string userId)
    {
        menu.UpdatedBy = userId;
        menu.UpdatedTime = clock.GetLocalNow().DateTime;
    }
}
