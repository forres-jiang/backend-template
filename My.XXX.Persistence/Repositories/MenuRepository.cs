using FluentResults;
using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Mapping;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.Models;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Persistence.Repositories;

public sealed class MenuRepository(DBContext db) : IMenuReadRepository, IPermissionStore, IMenuTransaction
{
    private readonly PersistenceMapper mapper = new();
    private bool writing;

    public Result Execute(Func<IMenuWriteSession, Result> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (writing) throw new InvalidOperationException("Nested menu transactions are not supported; compose operations within the existing session.");
        writing = true;
        var session = new WriteSession(db, mapper);
        try
        {
            return AtomicWrite.Execute(db, () =>
            {
                // Lock before any application reads; revision and data commit or roll back together.
                if (db.GetTable<PermissionRevision>().Where(r => r.Id == 1)
                    .Set(r => r.Version, r => r.Version + 1).Update() != 1)
                    throw new InvalidOperationException("Apply the PermissionRevision database upgrade before using menu writes.");
                return operation(session);
            }, result => result.IsSuccess);
        }
        finally { session.Close(); writing = false; }
    }

    public Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default) =>
        db.GetTable<PermissionRevision>().Where(r => r.Id == 1).Select(r => r.Version).SingleAsync(cancellationToken);
    private IQueryable<Menus> RoleMenus(List<Guid> roleIds) =>
        (from rm in db.RoleMenu
         join m in db.Menus on rm.MenuId equals m.Id
         where roleIds.Contains(rm.RoleId) && !rm.IsDeleted && !m.IsDeleted
         select m).Distinct();
    private sealed class PermissionRow
    {
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Url { get; set; }
    }
    private IQueryable<PermissionRow> PermissionRows(List<Guid> roleIds) => RoleMenus(roleIds)
        .Select(m => new PermissionRow { ControllerName = m.ControllerName, ActionName = m.ActionName, Url = m.Url });
    private static List<string> Paths(IEnumerable<PermissionRow> rows) => rows.Select(m =>
        !string.IsNullOrEmpty(m.ControllerName) && !string.IsNullOrEmpty(m.ActionName)
            ? m.ControllerName + "/" + m.ActionName : m.Url).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
    public List<string> GetPermissionPaths(List<Guid> roleIds) => Paths(PermissionRows(roleIds).ToList());
    public async Task<List<string>> GetPermissionPathsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default) =>
        Paths(await PermissionRows(roleIds).ToListAsync(cancellationToken));
    public MenuState Get(int id) => mapper.ToMenuState(db.Menus.FirstOrDefault(m => m.Id == id && !m.IsDeleted));
    public List<MenuState> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null)
    {
        var query = db.Menus.Where(m => !m.IsDeleted);
        if (parentId.HasValue) query = query.Where(m => m.ParentId == parentId.Value);
        if (ids != null) query = query.Where(m => ids.Contains(m.Id));
        if (isDisplay.HasValue) query = query.Where(m => m.IsDisplay == isDisplay.Value);
        return mapper.ToMenuStates(query.ToList());
    }
    public List<MenuState> GetRoleMenuByRoles(List<Guid> roleIds) => mapper.ToMenuStates(RoleMenus(roleIds).ToList());
    public Paged<MenuState> Search(MenuSearch query)
    {
        var menus = db.Menus.Where(m => !m.IsDeleted);
        if (query.IsAction.HasValue) menus = menus.Where(m => m.IsAction == query.IsAction.Value);
        if (query.IsDisplay) menus = menus.Where(m => m.IsDisplay);
        if (!string.IsNullOrEmpty(query.DisplayName)) menus = menus.Where(m => m.DisplayNames.Contains(query.DisplayName));
        if (query.ParentId.HasValue) menus = menus.Where(m => m.ParentId == query.ParentId.Value);
        var total = menus.Count();
        var list = menus.OrderByDescending(m => m.CreatedTime).ThenBy(m => m.Id)
            .Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();
        return Paged<MenuState>.Create(mapper.ToMenuStates(list), total);
    }

    private sealed class WriteSession(DBContext db, PersistenceMapper mapper) : IMenuWriteSession
    {
        private bool active = true;
        public void Close() => active = false;
        private void Check() { if (!active) throw new InvalidOperationException("The menu write session has ended."); }
        public List<MenuState> LoadMenus() { Check(); return mapper.ToMenuStates(db.Menus.Where(m => !m.IsDeleted).ToList()); }
        public List<int> LoadRoleMenus(Guid roleId)
        {
            Check();
            return db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted).Select(m => m.MenuId).ToList();
        }
        public int Insert(MenuState menu) { Check(); return db.Insert(mapper.ToMenuEntity(menu)); }
        public int Update(MenuState menu)
        {
            Check();
            // Explicit columns prevent accidental persistence of future application-only fields.
            return db.Menus.Where(m => m.Id == menu.Id && !m.IsDeleted)
                .Set(m => m.DisplayName, menu.DisplayName).Set(m => m.DisplayNames, menu.DisplayNames)
                .Set(m => m.Description, menu.Description).Set(m => m.Icon, menu.Icon)
                .Set(m => m.Url, menu.Url).Set(m => m.Component, menu.Component)
                .Set(m => m.ControllerName, menu.ControllerName).Set(m => m.ActionName, menu.ActionName)
                .Set(m => m.LinkTarget, menu.LinkTarget).Set(m => m.Number, menu.Number)
                .Set(m => m.ParentId, menu.ParentId).Set(m => m.IsDisplay, menu.IsDisplay).Set(m => m.IsAction, menu.IsAction)
                .Set(m => m.UpdatedBy, menu.UpdatedBy).Set(m => m.UpdatedTime, menu.UpdatedTime).Update();
        }
        public int Remove(List<int> ids, string userId, DateTime timestamp)
        {
            Check();
            return db.Menus.Where(m => !m.IsDeleted && ids.Contains(m.Id)).Set(m => m.IsDeleted, true)
                .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, timestamp).Update();
        }
        public bool ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp)
        {
            Check();
            if (removals.Count > 0 && db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted && removals.Contains(m.MenuId))
                .Set(m => m.IsDeleted, true).Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, timestamp).Update() != removals.Count) return false;
            foreach (var id in additions)
                if (db.Insert(new RoleMenu { RoleId = roleId, MenuId = id, CreatedBy = userId, CreatedTime = timestamp }) != 1) return false;
            return true;
        }
    }
}
