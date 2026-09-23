using FluentResults;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Services.Authorization;

public sealed class PermissionAdministration(IRolePermissionStore store, IAccessControlTransaction transaction,
    PermissionMutations mutations) : IPermissionAdministration
{
    public Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default) => store.GetAsync(roleId, cancellationToken);
    public Task<Result> ReplaceAsync(Guid roleId, List<string> codes, CancellationToken cancellationToken = default)
    {
        var validation = PermissionMutations.Validate(roleId, codes);
        return validation.IsFailed ? Task.FromResult(validation)
            : transaction.Execute(session => mutations.ReplaceAsync(session, roleId, codes, cancellationToken), cancellationToken);
    }
}
