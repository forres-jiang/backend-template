using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Interfaces;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Persistence.Repositories
{
    public class MenuRepository : IMenuRepository, IScopeDependency
    {
        private readonly DBContext _dbContext;

        public MenuRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Menus> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null)
        {
            var query = _dbContext.Menus.Where(m => !m.IsDeleted);
            if (parentId.HasValue) query = query.Where(m => m.ParentId == parentId.Value);
            if (ids != null) query = query.Where(m => ids.Contains(m.Id));
            if (isDisplay.HasValue) query = query.Where(m => m.IsDisplay == isDisplay.Value);
            return query.ToList();
        }

        public bool AddRoleMenu(RoleMenu model)
        {
            model.IsDeleted = false;
            var value = _dbContext.Insert(model);
            return value > 0;
        }

        public bool DeleteRoleMenu(RoleMenu model)
        {
            model.IsDeleted = true;
            var value = _dbContext.RoleMenu.Where(m => m.RoleId == model.RoleId
            && m.MenuId == model.MenuId && !m.IsDeleted)
                 .Set(m => m.IsDeleted, true)
                 .Set(m => m.UpdatedTime, DateTime.Now)
                 .Set(m => m.UpdatedBy, model.UpdatedBy)
                 .Update();
            return value > 0;
        }

        public List<Menus> GetRoleMenuByRoles(List<Guid> roleIds)
        {
            var roleMenus = (from rm in _dbContext.RoleMenu
                             join m in _dbContext.Menus on rm.MenuId equals m.Id
                             where roleIds.Contains(rm.RoleId)
                             && !rm.IsDeleted && !m.IsDeleted
                             select m).Distinct();
            return roleMenus.ToList();
        }

        public List<RoleMenu> GetRoleMenu(Guid? roleId = null)
        {
            var query = _dbContext.RoleMenu.Where(m => !m.IsDeleted);
            if (roleId.HasValue) query = query.Where(m => m.RoleId == roleId.Value);
            return query.ToList();
        }

        public bool RemoveRoleMenu(List<Guid> roleIds, List<int> menuIds, string userId)
        {
            var value = _dbContext.RoleMenu.Where(m => !m.IsDeleted
            && roleIds.Contains(m.RoleId) && menuIds.Contains(m.MenuId))
                 .Set(m => m.IsDeleted, true)
                 .Set(m => m.UpdatedTime, DateTime.Now)
                 .Set(m => m.UpdatedBy, userId)
                 .Update();
            return value > 0;
        }

        public Menus Get(int id) => _dbContext.Menus.FirstOrDefault(m => m.Id == id && !m.IsDeleted);
        public int Insert(Menus menu) => _dbContext.Insert(menu);
        public int Remove(List<int> ids, string userId) => _dbContext.Menus
            .Where(m => !m.IsDeleted && ids.Contains(m.Id)).Set(m => m.IsDeleted, true)
            .Set(m => m.UpdatedTime, DateTime.Now).Set(m => m.UpdatedBy, userId).Update();

        public int Update(EditMenu menu, string displayNames, string userId)
        {
            var statement = _dbContext.Menus.Where(m => m.Id == menu.Id && !m.IsDeleted)
                .Set(m => m.UpdatedTime, DateTime.Now).Set(m => m.UpdatedBy, userId);
            statement = new UpdateHelper<Menus>(statement).GetCondition(menu);
            if (displayNames != null) statement = statement.Set(m => m.DisplayNames, displayNames);
            return statement.Update();
        }

        public Paged<Menus> Search(QueryMenu query)
        {
            var menus = _dbContext.Menus.Where(m => !m.IsDeleted);
            if (query.IsAction.HasValue) menus = menus.Where(m => m.IsAction == query.IsAction.Value);
            if (!string.IsNullOrEmpty(query.DisplayName)) menus = menus.Where(m => m.DisplayNames.Contains(query.DisplayName));
            if (query.ParentId.HasValue) menus = menus.Where(m => m.ParentId == query.ParentId.Value);
            var total = menus.Count();
            var list = menus.OrderByDescending(m => m.CreatedTime).ThenBy(m => m.Id)
                .Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();
            return Paged<Menus>.Create(list, total);
        }

        public bool SaveRoleChanges(Guid roleId, List<int> removeIds, List<RoleMenu> additions, string userId) =>
            AtomicWrite.Execute(_dbContext, () =>
            {
                if (removeIds.Count > 0 && !RemoveRoleMenu(new List<Guid> { roleId }, removeIds, userId)) return false;
                return additions.Count == 0 || _dbContext.BulkCopy(additions).RowsCopied == additions.Count;
            }, success => success);

        public BatchWriteSummary ReplaceRoleActions(Guid roleId, List<RoleMenu> relations, string userId) =>
            AtomicWrite.Execute(_dbContext, () =>
            {
                _dbContext.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted)
                    .Set(m => m.IsDeleted, true).Set(m => m.UpdatedTime, DateTime.Now)
                    .Set(m => m.UpdatedBy, userId).Update();
                return _dbContext.BulkCopy(relations).ToSummary();
            }, result => result.RowsCopied == relations.Count);

        public bool SaveOrder(List<Menus> menus, string userId) => AtomicWrite.Execute(_dbContext, () =>
        {
            var now = DateTime.Now;
            for (var i = 0; i < menus.Count; i++)
                if (_dbContext.Menus.Where(m => m.Id == menus[i].Id && !m.IsDeleted)
                    .Set(m => m.Number, i).Set(m => m.UpdatedTime, now).Set(m => m.UpdatedBy, userId)
                    .Set(m => m.ParentId, menus[i].ParentId).Update() == 0) return false;
            return true;
        }, success => success);
    }
}