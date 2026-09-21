using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.Services.Interfaces
{
    public interface IMenuService
    {
        Task<Result> Add(SaveMenu menu, CancellationToken cancellationToken = default);

        public Task<Result> Remove(List<int> ids, CancellationToken cancellationToken = default);

        public Task<Result> Update(EditMenu menu, CancellationToken cancellationToken = default);

        public Task<MenuBaseDto> Get(int menuId, CancellationToken cancellationToken = default);

        public Task<Result> RoleMenus(Guid roleId, List<int> menuIds, bool isFull, CancellationToken cancellationToken = default);

        public Task<Result> RemoveRoleMenu(Guid roleId, int menuId, CancellationToken cancellationToken = default);

        public Task<Result<BatchWriteSummary>> RoleMenuAction(RoleMenuActionModel model, CancellationToken cancellationToken = default);

        public Task<Result<List<MenuBase>>> GetMenus(CancellationToken cancellationToken = default);

        public Task<Paged<MenuBaseDto>> GetMenus(QueryMenu query, CancellationToken cancellationToken = default);

        public Task<Result> UpdateSort(MenuSortModel model, CancellationToken cancellationToken = default);

        public Task<List<MenuDto>> GetMenuByRoles(RoleMenuQuery query, CancellationToken cancellationToken = default);

        public Task<List<MenuDto>> GetMenuTreeCheckedByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default);

        public Task<List<MenuDto>> GetTreeMenus(bool? isDisplay, CancellationToken cancellationToken = default);

        public Task<Result> RoleMenuRelation(InputRoleMenu input, CancellationToken cancellationToken = default);

        public Task<Paged<MenuSearchPickerDto>> SearchMenus(QueryMenu query, CancellationToken cancellationToken = default);



        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Result> RoleMenusRelation(InputRoleMenus input, CancellationToken cancellationToken = default);
    }
}