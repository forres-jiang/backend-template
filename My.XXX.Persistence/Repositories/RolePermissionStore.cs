using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Persistence.Repositories;
public sealed class RolePermissionStore(DBContext db) : IRolePermissionStore
{
    public Task<List<string>> GetAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        db.GetTable<RolePermission>().Where(p => p.RoleId == roleId).Select(p => p.Code).ToListAsync(cancellationToken);
    public async Task ReplaceAsync(Guid roleId, List<string> codes, CancellationToken cancellationToken = default)
    {
        await AtomicWrite.ExecuteAsync(db, async () =>
        {
            if (await db.GetTable<PermissionRevision>().Where(r => r.Id == 1)
                .Set(r => r.Version, r => r.Version + 1).UpdateAsync(cancellationToken) != 1)
                throw new InvalidOperationException("Permission schema is missing.");
            await db.GetTable<RolePermission>().Where(p => p.RoleId == roleId).DeleteAsync(cancellationToken);
            foreach (var code in codes)
                await db.InsertAsync(new RolePermission { RoleId = roleId, Code = code }, token: cancellationToken);
            return true;
        }, success => success, cancellationToken);
    }
}
