using My.XXX.Data.PersistantObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Data.Interfaces
{
    public interface IMenuRepository
    {
        public IQueryable<Menus> GetMenus();
        public bool AddRoleMenu(RoleMenu model);
        public IQueryable<Menus> GetRoleMenuByRoles(List<Guid> roleIds);
        public bool DeleteRoleMenu(RoleMenu model);
        public IQueryable<RoleMenu> GetRoleMenu();
        public bool RemoveRoleMenu(List<Guid> roleId, List<int> menuIds, string userId);
    }
}