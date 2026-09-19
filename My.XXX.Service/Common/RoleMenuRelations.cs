using My.XXX.Data.PersistantObjects;
using My.XXX.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service.Common;

public static class RoleMenuRelations
{
    public static List<RoleMenu> Build(RoleMenuActionModel model, string userId, DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.RoleId == Guid.Empty || model.Menus == null || model.Menus.Count == 0 ||
            model.Menus.Any(menu => menu == null || menu.MenuId <= 0 ||
                (menu.ActionIds != null && menu.ActionIds.Any(id => id <= 0))))
            throw new ArgumentException("Invalid role or menu selection.", nameof(model));

        return model.Menus
            .SelectMany(menu => (menu.ActionIds ?? new List<int>()).Append(menu.MenuId))
            .Distinct()
            .Select(id => new RoleMenu
            {
                RoleId = model.RoleId,
                MenuId = id,
                CreatedBy = userId,
                CreatedTime = timestamp,
                IsDeleted = false
            }).ToList();
    }
}
