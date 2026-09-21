using My.XXX.Services.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Common;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.APIs.Controllers
{
    [Authorize("Permissions")]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpPost("add")]
        [RequiresPermission(PermissionCodes.MenuAdd)]
        public async Task<MyResult> Add(SaveMenu model)
        {
            return (await _menuService.Add(model, HttpContext.RequestAborted)).ToApiResult();
        }

        [HttpDelete("remove")]
        [RequiresPermission(PermissionCodes.MenuRemove)]
        public async Task<MyResult> Remove(InputRemoveMenu model)
        {
            return (await _menuService.Remove(model.MenuIds, HttpContext.RequestAborted)).ToApiResult();
        }

        [HttpPatch("edit")]
        [RequiresPermission(PermissionCodes.MenuEdit)]
        public async Task<MyResult> Edit(EditMenu model)
        {
            return (await _menuService.Update(model, HttpContext.RequestAborted)).ToApiResult();
        }

        [HttpGet("Get/{menuId}")]
        [RequiresPermission(PermissionCodes.MenuGet)]
        public async Task<MyResult<MenuBaseDto>> Get(int menuId)
        {
            return MyResult<MenuBaseDto>.Success((await _menuService.Get(menuId, HttpContext.RequestAborted)));
        }

        [HttpPost("List")]
        [RequiresPermission(PermissionCodes.MenuList)]
        public async Task<MyResult<Paged<MenuBaseDto>>> List(QueryMenu query)
        {
            var result = (await _menuService.GetMenus(query, HttpContext.RequestAborted));
            return MyResult<Paged<MenuBaseDto>>.Success(result);
        }

        [HttpPost("Search")]
        [RequiresPermission(PermissionCodes.MenuSearch)]
        public async Task<MyResult<Paged<MenuSearchPickerDto>>> Search(QueryMenu query)
        {
            var result = (await _menuService.SearchMenus(query, HttpContext.RequestAborted));
            return MyResult<Paged<MenuSearchPickerDto>>.Success(result);
        }

        [HttpPost("RoleMenu")]
        [RequiresPermission(PermissionCodes.MenuRoleMenu)]
        public async Task<MyResult> RoleMenu(InputRoleMenu model)
        {
            var result = (await _menuService.RoleMenuRelation(model, HttpContext.RequestAborted));
            return result.ToApiResult();
        }

        [HttpPost("RoleMenus")]
        [RequiresPermission(PermissionCodes.MenuRoleMenus)]
        public async Task<MyResult<bool>> RoleMenus(RoleMenuIds model)
        {
            return (await _menuService.RoleMenus(model.RoleId, model.MenuIds, true, HttpContext.RequestAborted)).ToBooleanApiResult();
        }

        [HttpPost("RoleMenuChecked")]
        [RequiresPermission(PermissionCodes.MenuRoleMenuChecked)]
        public async Task<MyResult<bool>> RoleMenuChecked(InputRoleMenus model)
        {
            return (await _menuService.RoleMenusRelation(model, HttpContext.RequestAborted)).ToBooleanApiResult();
        }

        [HttpPost("RemoveRoleMenu")]
        [RequiresPermission(PermissionCodes.MenuRemoveRoleMenu)]
        public async Task<MyResult> RemoveRoleMenu(RemoveRoleMenu rrm)
        {
            return (await _menuService.RemoveRoleMenu(rrm.RoleId, rrm.MenuId, HttpContext.RequestAborted)).ToApiResult();
        }

        [HttpPost("Tree")]
        [RequiresPermission(PermissionCodes.MenuTree)]
        public async Task<MyResult<List<MenuDto>>> Tree()
        {
            var result = (await _menuService.GetTreeMenus(null, HttpContext.RequestAborted));
            return MyResult<List<MenuDto>>.Success(result);
        }

        [HttpPost("DisplayTree")]
        [RequiresPermission(PermissionCodes.MenuDisplayTree)]
        public async Task<MyResult<List<MenuDto>>> DisplayTree()
        {
            var result = (await _menuService.GetTreeMenus(true, HttpContext.RequestAborted));
            return MyResult<List<MenuDto>>.Success(result);
        }

        [HttpPost("TreeByRoleId")]
        [RequiresPermission(PermissionCodes.MenuTreeByRoleId)]
        public async Task<MyResult<List<MenuDto>>> GetMenuTreeByRoleId(RoleMenuBase model)
        {
            var result = (await _menuService.GetMenuTreeCheckedByRoles(new List<Guid> { model.RoleId }, HttpContext.RequestAborted));
            return MyResult<List<MenuDto>>.Success(result);
        }

        [HttpPost("TreeByRoleIds")]
        [RequiresPermission(PermissionCodes.MenuTreeByRoleIds)]
        public async Task<MyResult<List<MenuDto>>> GetMenuTreeByRoleIds(RolesMenuModel model)
        {
            var result = (await _menuService.GetMenuTreeCheckedByRoles(model.Ids, HttpContext.RequestAborted));
            return MyResult<List<MenuDto>>.Success(result);
        }

        [HttpPatch("UpdateSort")]
        [RequiresPermission(PermissionCodes.MenuUpdateSort)]
        public async Task<MyResult> UpdateSort(MenuSortModel model)
        {
            var result = (await _menuService.UpdateSort(model, HttpContext.RequestAborted));
            return result.ToApiResult();
        }
    }
}
