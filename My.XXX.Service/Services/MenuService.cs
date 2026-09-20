using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
namespace My.XXX.Service;
/// <summary>Compatibility facade for existing HTTP contracts.</summary>
public sealed class MenuService(MenuCommandService commands, MenuQueryService queries, RolePermissionService roles) : IMenuService, IScopeDependency
{
    public Result Add(SaveMenu menu) => commands.Add(menu);
    public Result Update(EditMenu menu) => commands.Update(menu);
    public Result Remove(List<int> ids) => commands.Remove(ids);
    public Result UpdateSort(MenuSortModel model) => commands.UpdateSort(model);
    public MenuBaseDto Get(int id) => queries.Get(id);
    public Result<List<MenuBase>> GetMenus() => queries.GetMenus();
    public Paged<MenuBaseDto> GetMenus(QueryMenu query) => queries.GetMenus(query);
    public List<MenuDto> GetTreeMenus(bool? isDisplay) => queries.GetTreeMenus(isDisplay);
    public List<MenuDto> GetMenuByRoles(RoleMenuQuery query) => queries.GetMenuByRoles(query);
    public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> ids) => queries.GetMenuTreeCheckedByRoles(ids);
    public Paged<MenuSearchPickerDto> SearchMenus(QueryMenu query) => queries.SearchMenus(query);
    public Result RoleMenus(Guid roleId, List<int> ids, bool isFull) => roles.RoleMenus(roleId, ids, isFull);
    public Result RoleMenuRelation(InputRoleMenu input) => roles.RoleMenuRelation(input);
    public Result RoleMenusRelation(InputRoleMenus input) => roles.RoleMenusRelation(input);
    public Result RemoveRoleMenu(Guid roleId, int menuId) => roles.RemoveRoleMenu(roleId, menuId);
    public Result<BatchWriteSummary> RoleMenuAction(RoleMenuActionModel model) => roles.RoleMenuAction(model);
}
