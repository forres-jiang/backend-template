using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System.Collections.Generic;

namespace My.XXX.Service;

public sealed class MenuCommandService(MenuMutations mutations, ICurrentUser user, ICurrentCulture culture)
{
    public Result Add(SaveMenu menu) => mutations.Add(menu, culture.CultureName, user.User?.UserId);
    public Result Update(EditMenu menu) => mutations.Update(menu, culture.CultureName, user.User?.UserId);
    public Result Remove(List<int> ids) => mutations.Remove(ids, user.User?.UserId);
    public Result UpdateSort(MenuSortModel model) => mutations.Move(model, user.User?.UserId);
}
