using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Service.Common;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using System.Collections.Generic;
namespace My.XXX.Service;

public sealed class MenuCommandService(IMenuRepository repository, ICurrentRequest current)
{
    public Result Add(SaveMenu menu)
    {
        if (menu == null || string.IsNullOrWhiteSpace(menu.DisplayName) || !menu.IsAction.HasValue || menu.ParentId < 0)
            return Result.Fail("Invalid menu.");
        return Result.OkIf(repository.Insert(menu, current.CultureName, current.User.UserId) > 0, "Add data failed.");
    }
    public Result Update(EditMenu menu)
    {
        if (menu == null || menu.Id <= 0 || menu.ParentId < 0 || !MenuUpdateFields.Valid(menu.ClearFields)) return Result.Fail("Invalid menu update.");
        return Result.OkIf(repository.Update(menu, current.CultureName, current.User.UserId) > 0, "Update failed.");
    }
    public Result Remove(List<int> ids) => ids == null || ids.Count == 0
        ? Result.Fail("Id cannot be empty") : Result.OkIf(repository.Remove(ids, current.User.UserId) > 0, "Delete failed.");
    public Result UpdateSort(MenuSortModel model) => Result.OkIf(repository.Move(model, current.User.UserId), "Save failed.");
}
