using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Services.Interfaces;
using System.Collections.Generic;

namespace My.XXX.Services;

public sealed class MenuCommandService(MenuMutations mutations, ICurrentUser user, ICurrentCulture culture)
{
    public async Task<Result> Add(SaveMenu menu, CancellationToken cancellationToken = default) => (await mutations.Add(menu, culture.CultureName, user.User?.UserId, cancellationToken));
    public async Task<Result> Update(EditMenu menu, CancellationToken cancellationToken = default) => (await mutations.Update(menu, culture.CultureName, user.User?.UserId, cancellationToken));
    public async Task<Result> Remove(List<int> ids, CancellationToken cancellationToken = default) => (await mutations.Remove(ids, user.User?.UserId, cancellationToken));
    public async Task<Result> UpdateSort(MenuSortModel model, CancellationToken cancellationToken = default) => (await mutations.Move(model, user.User?.UserId, cancellationToken));
}
