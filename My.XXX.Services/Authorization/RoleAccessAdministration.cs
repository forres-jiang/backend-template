using FluentResults;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authorization;

/// <summary>Replace menu selection and permission codes as one atomic use case.</summary>
public sealed class RoleAccessAdministration(IAccessControlTransaction transaction, RoleMenuMutations menus,
    PermissionMutations permissions, ICurrentUser current)
{
    public Task<Result> ReplaceAsync(Guid roleId, List<int> menuIds, List<string> codes,
        CancellationToken cancellationToken = default) => transaction.Execute(async session =>
    {
        var result = await menus.SetRoleMenus(session, roleId, menuIds, RoleMenuChange.Replace,
            current.User?.UserId, cancellationToken);
        if (result.IsFailed) return result;
        return await permissions.ReplaceAsync(session, roleId, codes, cancellationToken);
    }, cancellationToken);
}
