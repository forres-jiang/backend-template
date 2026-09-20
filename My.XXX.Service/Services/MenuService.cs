using System.Threading;
using System.Threading.Tasks;
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
    public async Task<Result> Add(SaveMenu menu, CancellationToken cancellationToken = default) => (await commands.Add(menu, cancellationToken));
    public async Task<Result> Update(EditMenu menu, CancellationToken cancellationToken = default) => (await commands.Update(menu, cancellationToken));
    public async Task<Result> Remove(List<int> ids, CancellationToken cancellationToken = default) => (await commands.Remove(ids, cancellationToken));
    public async Task<Result> UpdateSort(MenuSortModel model, CancellationToken cancellationToken = default) => (await commands.UpdateSort(model, cancellationToken));
    public async Task<MenuBaseDto> Get(int id, CancellationToken cancellationToken = default) => (await queries.Get(id, cancellationToken));
    public async Task<Result<List<MenuBase>>> GetMenus(CancellationToken cancellationToken = default) => (await queries.GetMenus(cancellationToken));
    public async Task<Paged<MenuBaseDto>> GetMenus(QueryMenu query, CancellationToken cancellationToken = default) => (await queries.GetMenus(query, cancellationToken));
    public async Task<List<MenuDto>> GetTreeMenus(bool? isDisplay, CancellationToken cancellationToken = default) => (await queries.GetTreeMenus(isDisplay, cancellationToken));
    public async Task<List<MenuDto>> GetMenuByRoles(RoleMenuQuery query, CancellationToken cancellationToken = default) => (await queries.GetMenuByRoles(query, cancellationToken));
    public async Task<List<MenuDto>> GetMenuTreeCheckedByRoles(List<Guid> ids, CancellationToken cancellationToken = default) => (await queries.GetMenuTreeCheckedByRoles(ids, cancellationToken));
    public async Task<Paged<MenuSearchPickerDto>> SearchMenus(QueryMenu query, CancellationToken cancellationToken = default) => (await queries.SearchMenus(query, cancellationToken));
    public async Task<Result> RoleMenus(Guid roleId, List<int> ids, bool isFull, CancellationToken cancellationToken = default) => (await roles.RoleMenus(roleId, ids, isFull, cancellationToken));
    public async Task<Result> RoleMenuRelation(InputRoleMenu input, CancellationToken cancellationToken = default) => (await roles.RoleMenuRelation(input, cancellationToken));
    public async Task<Result> RoleMenusRelation(InputRoleMenus input, CancellationToken cancellationToken = default) => (await roles.RoleMenusRelation(input, cancellationToken));
    public async Task<Result> RemoveRoleMenu(Guid roleId, int menuId, CancellationToken cancellationToken = default) => (await roles.RemoveRoleMenu(roleId, menuId, cancellationToken));
    public async Task<Result<BatchWriteSummary>> RoleMenuAction(RoleMenuActionModel model, CancellationToken cancellationToken = default) => (await roles.RoleMenuAction(model, cancellationToken));
}
