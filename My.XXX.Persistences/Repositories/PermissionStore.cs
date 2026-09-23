using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Persistences.Repositories;

public sealed class PermissionStore(DBContext db) : IPermissionStore
{
    public Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default) =>
        db.GetTable<PermissionRevision>().Where(r => r.Id == 1).Select(r => r.Version).SingleAsync(cancellationToken);
    private IQueryable<string> Codes(List<Guid> roleIds) => db.GetTable<RolePermission>()
        .Where(p => roleIds.Contains(p.RoleId)).Select(p => p.Code).Distinct();
    public List<string> GetPermissionCodes(List<Guid> roleIds) => Codes(roleIds).ToList();
    public Task<List<string>> GetPermissionCodesAsync(List<Guid> roleIds, CancellationToken cancellationToken = default) =>
        Codes(roleIds).ToListAsync(cancellationToken);
}
