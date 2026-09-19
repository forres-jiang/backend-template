using My.XXX.Data.PersistantObjects;
using LinqToDB.Data;
using FluentResults;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using System;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces
{
    public interface IMenuService
    {
        Result Add(SaveMenu menu);

        public Result Remove(List<int> ids);

        public Result Update(EditMenu menu);

        public MenuBaseDto Get(int menuId);

        public bool RoleMenus(Guid roleId, List<int> menuIds, bool isFull);

        public Result RemoveRoleMenu(Guid roleId, int menuId);

        public Result<BulkCopyRowsCopied> RoleMenuAction(RoleMenuActionModel model);

        public Result<List<Menus>> GetMenus();

        public Paged<MenuBaseDto> GetMenus(QueryMenu query);

        public bool UpdateSort(MenuSortModel model);

        public List<MenuDto> GetMenuByRoles(RoleMenuQuery query);

        public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> roleIds);

        public List<MenuDto> GetTreeMenus(bool? isDisplay);

        public bool RoleMenuRelation(InputRoleMenu input);

        public Paged<MenuSearchPickerDto> SearchMenus(QueryMenu query);

        public List<string> GetRoleMenuPaths(List<Guid> roleIds);

        public List<string> GetRoleMenuPaths(List<Guid> roleIds, string userId);

        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool RoleMenusRelation(InputRoleMenus input);
    }
}