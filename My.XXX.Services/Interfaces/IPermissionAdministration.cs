using FluentResults;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Services.Interfaces;
public interface IPermissionAdministration
{
    Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Result> ReplaceAsync(Guid roleId, List<string> codes, CancellationToken cancellationToken = default);
}
