using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.AccessControl.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Menus;

/// <summary>Owns the request context and transaction boundary for menu commands.</summary>
public sealed class MenuCommandService(IAccessControlTransaction transaction, MenuMutations mutations,
    ICurrentUser user, ICurrentCulture culture)
{
    public Task<Result> Add(SaveMenu menu, CancellationToken cancellationToken = default) =>
        Execute(MenuMutations.ValidateAdd(menu), session => mutations.Add(session, menu, culture.CultureName, user.User?.UserId, cancellationToken), cancellationToken);
    public Task<Result> Update(EditMenu menu, CancellationToken cancellationToken = default) =>
        Execute(MenuMutations.ValidateUpdate(menu), session => mutations.Update(session, menu, culture.CultureName, user.User?.UserId, cancellationToken), cancellationToken);
    public Task<Result> Remove(List<int> ids, CancellationToken cancellationToken = default) =>
        Execute(MenuMutations.ValidateRemove(ids), session => mutations.Remove(session, ids, user.User?.UserId, cancellationToken), cancellationToken);
    public Task<Result> UpdateSort(MenuSortModel model, CancellationToken cancellationToken = default) =>
        transaction.Execute(session => mutations.Move(session, model, user.User?.UserId, cancellationToken), cancellationToken);
    private Task<Result> Execute(Result validation, Func<IAccessControlWriteSession, Task<Result>> operation,
        CancellationToken cancellationToken) => validation.IsFailed ? Task.FromResult(validation)
        : transaction.Execute(operation, cancellationToken);
}
