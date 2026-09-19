using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
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

        public Result RoleMenus(Guid roleId, List<int> menuIds, bool isFull);

        public Result RemoveRoleMenu(Guid roleId, int menuId);

        public Result<BatchWriteSummary> RoleMenuAction(RoleMenuActionModel model);

        public Result<List<MenuBase>> GetMenus();

        public Paged<MenuBaseDto> GetMenus(QueryMenu query);

        public Result UpdateSort(MenuSortModel model);

        public List<MenuDto> GetMenuByRoles(RoleMenuQuery query);

        public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> roleIds);

        public List<MenuDto> GetTreeMenus(bool? isDisplay);

        public Result RoleMenuRelation(InputRoleMenu input);

        public Paged<MenuSearchPickerDto> SearchMenus(QueryMenu query);



        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Result RoleMenusRelation(InputRoleMenus input);
    }
}