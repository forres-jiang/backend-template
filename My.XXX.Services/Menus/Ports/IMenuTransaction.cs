using FluentResults;
using My.XXX.Services.Menus.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Menus.Ports;

public interface IMenuTransaction
{
    /// <summary>Locks the permission revision before invoking the use case. Failure results and exceptions roll back all writes.</summary>
    Task<Result> Execute(Func<IMenuWriteSession, Task<Result>> operation, CancellationToken cancellationToken = default);
}

/// <summary>Available only inside Execute. Contains persistence primitives, not business commands.</summary>
public interface IMenuWriteSession
{
    Task<List<MenuState>> LoadMenus(CancellationToken cancellationToken = default);
    Task<List<int>> LoadRoleMenus(Guid roleId, CancellationToken cancellationToken = default);
    Task<int> Insert(MenuState menu, CancellationToken cancellationToken = default);
    Task<int> Update(MenuState menu, CancellationToken cancellationToken = default);
    Task<int> Remove(List<int> ids, string userId, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp, CancellationToken cancellationToken = default);
}
