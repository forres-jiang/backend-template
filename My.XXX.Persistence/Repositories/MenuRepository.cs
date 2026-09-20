using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Mapping;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Persistence.Repositories;

public sealed class MenuRepository(DBContext db) : IMenuRepository, IScopeDependency
{
    private readonly PersistenceMapper mapper = new();
    // Acquire the singleton write lock before reading. Revision and data commit together.
    private T Write<T>(Func<T> action, Func<T, bool> succeeded) => AtomicWrite.Execute(db, () =>
    {
        if (db.GetTable<PermissionRevision>().Where(r => r.Id == 1)
            .Set(r => r.Version, r => r.Version + 1).Update() != 1)
            throw new InvalidOperationException("Apply the PermissionRevision database upgrade before using menu writes.");
        return action();
    }, succeeded);
    public Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default) =>
        db.GetTable<PermissionRevision>().Where(r => r.Id == 1).Select(r => r.Version).SingleAsync(cancellationToken);
    private IQueryable<Menus> RoleMenus(List<Guid> roleIds) =>
        (from rm in db.RoleMenu join m in db.Menus on rm.MenuId equals m.Id
         where roleIds.Contains(rm.RoleId) && !rm.IsDeleted && !m.IsDeleted select m).Distinct();
    public async Task<List<string>> GetPermissionPathsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var rows = await RoleMenus(roleIds).Select(m => new { m.ControllerName, m.ActionName, m.Url }).ToListAsync(cancellationToken);
        return rows.Select(m => !string.IsNullOrEmpty(m.ControllerName) && !string.IsNullOrEmpty(m.ActionName)
            ? m.ControllerName + "/" + m.ActionName : m.Url).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
    }
    public MenuBase Get(int id) => mapper.ToMenuBase(db.Menus.FirstOrDefault(m => m.Id == id && !m.IsDeleted));
    public List<MenuBase> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null)
    {
        var query = db.Menus.Where(m => !m.IsDeleted);
        if (parentId.HasValue) query = query.Where(m => m.ParentId == parentId.Value);
        if (ids != null) query = query.Where(m => ids.Contains(m.Id));
        if (isDisplay.HasValue) query = query.Where(m => m.IsDisplay == isDisplay.Value);
        return mapper.ToMenuBases(query.ToList());
    }
    public List<MenuBase> GetRoleMenuByRoles(List<Guid> roleIds) => mapper.ToMenuBases(RoleMenus(roleIds).ToList());
    public Paged<MenuBase> Search(QueryMenu query)
    {
        var menus = db.Menus.Where(m => !m.IsDeleted);
        if (query.IsAction.HasValue) menus = menus.Where(m => m.IsAction == query.IsAction.Value);
        if (query.IsDisplay) menus = menus.Where(m => m.IsDisplay);
        if (!string.IsNullOrEmpty(query.DisplayName)) menus = menus.Where(m => m.DisplayNames.Contains(query.DisplayName));
        if (query.ParentId.HasValue) menus = menus.Where(m => m.ParentId == query.ParentId.Value);
        var total = menus.Count();
        var list = menus.OrderByDescending(m => m.CreatedTime).ThenBy(m => m.Id)
            .Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();
        return Paged<MenuBase>.Create(mapper.ToMenuBases(list), total);
    }
    private bool ValidParent(int id, int parentId, bool isAction) => MenuHierarchy.CanPlace(GetMenus(), id, parentId, isAction);
    public int Insert(SaveMenu menu, string culture, string userId) => Write(() =>
    {
        if (!ValidParent(0, menu.ParentId, menu.IsAction == true)) return 0;
        var entity = mapper.ToMenu(menu);
        entity.ActionName = entity.ActionName?.Trim();
        entity.ControllerName = entity.ControllerName?.Trim();
        entity.DisplayNames = MenuDisplayNames.Set(null, culture, menu.DisplayName);
        entity.CreatedBy = entity.UpdatedBy = userId;
        entity.CreatedTime = DateTime.Now;
        entity.UpdatedTime = entity.CreatedTime;
        return db.Insert(entity);
    }, rows => rows > 0);
    public int Remove(List<int> ids, string userId) => Write(() => db.Menus
        .Where(m => !m.IsDeleted && ids.Contains(m.Id)).Set(m => m.IsDeleted, true)
        .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, DateTime.Now).Update(), rows => rows > 0);
    public int Update(EditMenu menu, string culture, string userId) => Write(() =>
    {
        if (!MenuUpdateFields.Valid(menu.ClearFields)) return 0;
        var old = db.Menus.FirstOrDefault(m => m.Id == menu.Id && !m.IsDeleted);
        if (old == null || !ValidParent(menu.Id, menu.ParentId ?? old.ParentId, menu.IsAction ?? old.IsAction)) return 0;
        var clear = new HashSet<string>(menu.ClearFields ?? new(), StringComparer.OrdinalIgnoreCase);
        var update = db.Menus.Where(m => m.Id == menu.Id && !m.IsDeleted)
            .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, DateTime.Now);
        if (!string.IsNullOrWhiteSpace(menu.DisplayName)) update = update
            .Set(m => m.DisplayName, menu.DisplayName.Trim())
            .Set(m => m.DisplayNames, MenuDisplayNames.Set(old.DisplayNames, culture, menu.DisplayName));
        if (menu.Description != null || clear.Contains(nameof(menu.Description))) update = update.Set(m => m.Description, clear.Contains(nameof(menu.Description)) ? null : menu.Description.Trim());
        if (menu.Icon != null || clear.Contains(nameof(menu.Icon))) update = update.Set(m => m.Icon, clear.Contains(nameof(menu.Icon)) ? null : menu.Icon.Trim());
        if (menu.Url != null || clear.Contains(nameof(menu.Url))) update = update.Set(m => m.Url, clear.Contains(nameof(menu.Url)) ? null : menu.Url.Trim());
        if (menu.Component != null || clear.Contains(nameof(menu.Component))) update = update.Set(m => m.Component, clear.Contains(nameof(menu.Component)) ? null : menu.Component.Trim());
        if (menu.ControllerName != null || clear.Contains(nameof(menu.ControllerName))) update = update.Set(m => m.ControllerName, clear.Contains(nameof(menu.ControllerName)) ? null : menu.ControllerName.Trim());
        if (menu.ActionName != null || clear.Contains(nameof(menu.ActionName))) update = update.Set(m => m.ActionName, clear.Contains(nameof(menu.ActionName)) ? null : menu.ActionName.Trim());
        if (menu.LinkTarget != null || clear.Contains(nameof(menu.LinkTarget))) update = update.Set(m => m.LinkTarget, clear.Contains(nameof(menu.LinkTarget)) ? null : menu.LinkTarget.Trim());
        if (menu.Number.HasValue) update = update.Set(m => m.Number, menu.Number.Value);
        if (menu.ParentId.HasValue) update = update.Set(m => m.ParentId, menu.ParentId.Value);
        if (menu.IsDisplay.HasValue) update = update.Set(m => m.IsDisplay, menu.IsDisplay.Value);
        if (menu.IsAction.HasValue) update = update.Set(m => m.IsAction, menu.IsAction.Value);
        return update.Update();
    }, rows => rows > 0);
    private bool SetRoleMenusCore(Guid roleId, List<int> ids, RoleMenuChange change, string userId)
    {
        if (roleId == Guid.Empty || ids == null || ids.Any(id => id <= 0)) return false;
        var selected = ids.Distinct().ToList();
        if (change != RoleMenuChange.Remove && db.Menus.Count(m => selected.Contains(m.Id) && !m.IsDeleted) != selected.Count) return false;
        var existing = db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted).Select(m => m.MenuId).ToList();
        var removals = change == RoleMenuChange.Replace ? existing.Except(selected).ToList()
            : change == RoleMenuChange.Remove ? existing.Intersect(selected).ToList() : new List<int>();
        if (removals.Count > 0) db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted && removals.Contains(m.MenuId))
            .Set(m => m.IsDeleted, true).Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, DateTime.Now).Update();
        if (change != RoleMenuChange.Remove)
            foreach (var id in selected.Except(existing))
                if (db.Insert(new RoleMenu { RoleId = roleId, MenuId = id, CreatedBy = userId, CreatedTime = DateTime.Now }) <= 0) return false;
        return true;
    }
    public bool SetRoleMenus(Guid roleId, List<int> menuIds, RoleMenuChange change, string userId) =>
        Write(() => SetRoleMenusCore(roleId, menuIds, change, userId), success => success);
    public BatchWriteSummary ReplaceRoleActions(Guid roleId, List<int> menuIds, string userId) => Write(() =>
    {
        var result = new BatchWriteSummary { StartTime = DateTime.Now };
        result.Abort = !SetRoleMenusCore(roleId, menuIds, RoleMenuChange.Replace, userId);
        result.RowsCopied = result.Abort ? 0 : menuIds.Distinct().Count();
        return result;
    }, result => !result.Abort);
    public bool Move(MenuSortModel model, string userId) => Write(() =>
    {
        var order = MenuOrder.Build(GetMenus(), model);
        if (order == null) return false;
        var current = order.First(m => m.Id == model.CurrentId);
        if (!ValidParent(current.Id, current.ParentId, current.IsAction)) return false;
        for (var i = 0; i < order.Count; i++)
            if (db.Menus.Where(m => m.Id == order[i].Id && !m.IsDeleted)
                .Set(m => m.Number, i).Set(m => m.ParentId, order[i].ParentId)
                .Set(m => m.UpdatedBy, userId).Set(m => m.UpdatedTime, DateTime.Now).Update() != 1) return false;
        return true;
    }, success => success);
}
