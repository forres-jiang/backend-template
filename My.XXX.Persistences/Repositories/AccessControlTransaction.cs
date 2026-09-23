using FluentResults;
using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistences.Common;
using My.XXX.Persistences.Mapping;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Menus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Persistences.Repositories;

public sealed class AccessControlTransaction(DBContext db) : IAccessControlTransaction
{
    private readonly PersistenceMapper mapper = new();
    private bool writing;

    public async Task<Result> Execute(Func<IAccessControlWriteSession, Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (writing) throw new InvalidOperationException("Nested menu transactions are not supported; compose operations within the existing session.");
        writing = true;
        var session = new WriteSession(db, mapper);
        try
        {
            return await AtomicWrite.ExecuteAsync(db, async () =>
            {
                // 在任何应用程序读取之前加锁；修订号与数据一起提交或回滚。
                if (await db.GetTable<PermissionRevision>().Where(r => r.Id == 1)
                    .Set(r => r.Version, r => r.Version + 1).UpdateAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("Apply the PermissionRevision database upgrade before using menu writes.");
                return await operation(session);
            }, result => result.IsSuccess, cancellationToken);
        }
        finally { session.Close(); writing = false; }
    }

    private sealed class WriteSession(DBContext db, PersistenceMapper mapper) : IAccessControlWriteSession
    {
        private bool active = true;
        public void Close() => active = false;
        private void Check() { if (!active) throw new InvalidOperationException("The menu write session has ended."); }
        public async Task ReplaceRolePermissions(Guid roleId, List<string> codes, CancellationToken cancellationToken = default)
        {
            Check();
            await db.GetTable<RolePermission>().Where(p => p.RoleId == roleId).DeleteAsync(cancellationToken);
            foreach (var code in codes)
                await db.InsertAsync(new RolePermission { RoleId = roleId, Code = code }, token: cancellationToken);
        }
        public async Task<List<MenuState>> LoadMenus(CancellationToken cancellationToken = default) { Check(); return mapper.ToMenuStates(await db.Menus.Where(m => !m.IsDeleted).ToListAsync(cancellationToken)); }
        public Task<List<int>> LoadMenuIds(CancellationToken cancellationToken = default) { Check(); return db.Menus.Where(m => !m.IsDeleted).Select(m => m.Id).ToListAsync(cancellationToken); }
        public async Task<List<int>> LoadRoleMenus(Guid roleId, CancellationToken cancellationToken = default)
        {
            Check();
            return await db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted).Select(m => m.MenuId).ToListAsync(cancellationToken);
        }
        public async Task<int> Insert(MenuState menu, CancellationToken cancellationToken = default) { Check(); return await db.InsertAsync(mapper.ToMenuEntity(menu), token: cancellationToken); }
        public async Task<int> Update(MenuState menu, CancellationToken cancellationToken = default)
        {
            Check();
            // 显式指定列可防止将来仅供应用程序使用的字段被意外持久化。
            return await db.Menus.Where(m => m.Id == menu.Id && !m.IsDeleted)
                .Set(m => m.DisplayName, menu.DisplayName).Set(m => m.DisplayNames, My.XXX.Contracts.Serialization.LocalizedNamesJson.Write(menu.DisplayNames?.Values))
                .Set(m => m.Description, menu.Description).Set(m => m.Icon, menu.Icon)
                .Set(m => m.Url, menu.Url).Set(m => m.Component, menu.Component)
                .Set(m => m.ControllerName, menu.ControllerName).Set(m => m.ActionName, menu.ActionName)
                .Set(m => m.LinkTarget, menu.LinkTarget).Set(m => m.Number, menu.Number)
                .Set(m => m.ParentId, menu.ParentId).Set(m => m.IsDisplay, menu.IsDisplay).Set(m => m.IsAction, menu.IsAction)
                .Set(m => m.UpdatedBy, menu.UpdatedBy).Set(m => m.UpdatedTime, menu.UpdatedTime).UpdateAsync(cancellationToken);
        }
        public async Task<int> Remove(List<int> ids, string userId, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            Check();
            await db.RoleMenu.Where(m => !m.IsDeleted && ids.Contains(m.MenuId)).Set(m => m.IsDeleted, true)
                .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, timestamp).UpdateAsync(cancellationToken);
            return await db.Menus.Where(m => !m.IsDeleted && ids.Contains(m.Id)).Set(m => m.IsDeleted, true)
                .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, timestamp).UpdateAsync(cancellationToken);
        }
        public async Task<bool> ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            Check();
            if (removals.Count > 0 && await db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted && removals.Contains(m.MenuId))
                .Set(m => m.IsDeleted, true).Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, timestamp).UpdateAsync(cancellationToken) != removals.Count) return false;
            foreach (var id in additions)
                if (await db.InsertAsync(new RoleMenu { RoleId = roleId, MenuId = id, CreatedBy = userId, CreatedTime = timestamp }, token: cancellationToken) != 1) return false;
            return true;
        }
    }
}
