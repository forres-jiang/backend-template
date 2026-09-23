using FluentResults;
using My.XXX.Services.Menus.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.AccessControl.Ports;

public interface IAccessControlTransaction
{
    /// <summary>在调用用例之前锁定权限修订版本。失败结果和异常会回滚所有写入。</summary>
    Task<Result> Execute(Func<IAccessControlWriteSession, Task<Result>> operation, CancellationToken cancellationToken = default);
}

/// <summary>仅在 Execute 内部可用。包含持久化原语，而非业务命令。</summary>
public interface IAccessControlWriteSession
{
    Task<List<MenuState>> LoadMenus(CancellationToken cancellationToken = default);
    Task<List<int>> LoadMenuIds(CancellationToken cancellationToken = default);
    Task<List<int>> LoadRoleMenus(Guid roleId, CancellationToken cancellationToken = default);
    Task<int> Insert(MenuState menu, CancellationToken cancellationToken = default);
    Task<int> Update(MenuState menu, CancellationToken cancellationToken = default);
    Task<int> Remove(List<int> ids, string userId, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp, CancellationToken cancellationToken = default);
    Task ReplaceRolePermissions(Guid roleId, List<string> codes, CancellationToken cancellationToken = default);
}
