using LinqToDB;
using My.XXX.Data.Interfaces;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Data.Repositories
{
    public class MenuRepository : IMenuRepository, IScopeDependency
    {
        private readonly DBContext _dbContext;

        public MenuRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<Menus> GetMenus()
        {
            return from m in _dbContext.Menus where !m.IsDeleted select m;
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

        public IQueryable<Menus> GetRoleMenuByRoles(List<Guid> roleIds)
        {
            var roleMenus = (from rm in _dbContext.RoleMenu
                             join m in _dbContext.Menus on rm.MenuId equals m.Id
                             where roleIds.Contains(rm.RoleId)
                             && !rm.IsDeleted && !m.IsDeleted
                             select m).Distinct();
            return roleMenus;
        }

        public IQueryable<RoleMenu> GetRoleMenu()
        {
            return _dbContext.RoleMenu.Where(m => !m.IsDeleted);
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
    }
}