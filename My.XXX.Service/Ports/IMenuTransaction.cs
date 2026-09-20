using FluentResults;
using My.XXX.Service.Models;
using System;
using System.Collections.Generic;

namespace My.XXX.Service.Ports;

public interface IMenuTransaction
{
    /// <summary>Locks the permission revision before invoking the use case. Failure results and exceptions roll back all writes.</summary>
    Result Execute(Func<IMenuWriteSession, Result> operation);
}

/// <summary>Available only inside Execute. Contains persistence primitives, not business commands.</summary>
public interface IMenuWriteSession
{
    List<MenuState> LoadMenus();
    List<int> LoadRoleMenus(Guid roleId);
    int Insert(MenuState menu);
    int Update(MenuState menu);
    int Remove(List<int> ids, string userId, DateTime timestamp);
    bool ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp);
}
