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

public sealed class RolePermissionStore(DBContext db) : IRolePermissionStore
{
    public Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        db.GetTable<RolePermission>().Where(p => p.RoleId == roleId).Select(p => p.Code).ToListAsync(cancellationToken);
}
