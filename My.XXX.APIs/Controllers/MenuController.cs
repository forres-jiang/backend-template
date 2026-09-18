using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
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
        public PwCResult Add(SaveMenu model)
        {
            return _menuService.Add(model);
        }

        [HttpDelete("remove")]
        public PwCResult Remove(InputRemoveMenu model)
        {
            return _menuService.Remove(model.MenuIds);
        }

        [HttpPatch("edit")]
        public PwCResult Edit(EditMenu model)
        {
            return _menuService.Update(model);
        }

        [HttpGet("Get/{menuId}")]
        public MenuBaseDto Get(int menuId)
        {
            return _menuService.Get(menuId);
        }

        [HttpPost("List")]
        public PwCResult List(QueryMenu query)
        {
            var result = _menuService.GetMenus(query);
            return PwCResult.Success(result);
        }

        [HttpPost("Search")]
        public PwCResult Search(QueryMenu query)
        {
            var result = _menuService.SearchMenus(query);
            return PwCResult.Success(result);
        }

        [HttpPost("RoleMenu")]
        public PwCResult RoleMenu(InputRoleMenu model)
        {
            var result = _menuService.RoleMenuRelation(model);
            return result ? PwCResult.Success() : PwCResult.Fail("Data save failed.");
        }

        [HttpPost("RoleMenus")]
        public bool RoleMenus(RoleMenuIds model)
        {
            return _menuService.RoleMenus(model.RoleId, model.MenuIds, true);
        }

        [HttpPost("RoleMenuChecked")]
        public bool RoleMenuChecked(InputRoleMenus model)
        {
            return _menuService.RoleMenusRelation(model);
        }

        [HttpPost("RemoveRoleMenu")]
        public PwCResult RemoveRoleMenu(RemoveRoleMenu rrm)
        {
            return _menuService.RemoveRoleMenu(rrm.RoleId, rrm.MenuId);
        }

        [HttpPost("Tree")]
        public List<MenuDto> Tree()
        {
            var result = _menuService.GetTreeMenus(null);
            return result;
        }

        [HttpPost("DisplayTree")]
        public List<MenuDto> DisplayTree()
        {
            var result = _menuService.GetTreeMenus(true);
            return result;
        }

        [HttpPost("TreeByRoleId")]
        public List<MenuDto> GetMenuTreeByRoleId(RoleMenuBase model)
        {
            var result = _menuService.GetMenuTreeCheckedByRoles(new List<Guid> { model.RoleId });
            return result;
        }

        [HttpPost("TreeByRoleIds")]
        public List<MenuDto> GetMenuTreeByRoleIds(RolesMenuModel model)
        {
            var result = _menuService.GetMenuTreeCheckedByRoles(model.Ids);
            return result;
        }

        [HttpPatch("UpdateSort")]
        public PwCResult UpdateSort(MenuSortModel model)
        {
            var result = _menuService.UpdateSort(model);
            return result ? PwCResult.Success() : PwCResult.Fail("Failed to adjust menu order.");
        }
    }
}