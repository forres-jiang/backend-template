using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.Persistence.Interfaces
{
    public interface IMenuRepository
    {
        Menus Get(int id);
        int Insert(Menus menu);
        int Remove(List<int> ids, string userId);
        int Update(EditMenu menu, string displayNames, string userId);
        bool SaveRoleChanges(Guid roleId, List<int> removeIds, List<RoleMenu> additions, string userId);
        BatchWriteSummary ReplaceRoleActions(Guid roleId, List<RoleMenu> relations, string userId);
        bool SaveOrder(List<Menus> menus, string userId);
        Paged<Menus> Search(QueryMenu query);
        public List<Menus> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null);
        public bool AddRoleMenu(RoleMenu model);
        public List<Menus> GetRoleMenuByRoles(List<Guid> roleIds);
        public bool DeleteRoleMenu(RoleMenu model);
        public List<RoleMenu> GetRoleMenu(Guid? roleId = null);
        public bool RemoveRoleMenu(List<Guid> roleId, List<int> menuIds, string userId);
    }
}