using My.XXX.APIs.Common;
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
        public MyResult Add(SaveMenu model)
        {
            return _menuService.Add(model).ToApiResult();
        }

        [HttpDelete("remove")]
        public MyResult Remove(InputRemoveMenu model)
        {
            return _menuService.Remove(model.MenuIds).ToApiResult();
        }

        [HttpPatch("edit")]
        public MyResult Edit(EditMenu model)
        {
            return _menuService.Update(model).ToApiResult();
        }

        [HttpGet("Get/{menuId}")]
        public MenuBaseDto Get(int menuId)
        {
            return _menuService.Get(menuId);
        }

        [HttpPost("List")]
        public MyResult List(QueryMenu query)
        {
            var result = _menuService.GetMenus(query);
            return MyResult.Success(result);
        }

        [HttpPost("Search")]
        public MyResult Search(QueryMenu query)
        {
            var result = _menuService.SearchMenus(query);
            return MyResult.Success(result);
        }

        [HttpPost("RoleMenu")]
        public MyResult RoleMenu(InputRoleMenu model)
        {
            var result = _menuService.RoleMenuRelation(model);
            return result ? MyResult.Success() : MyResult.Fail("Data save failed.");
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
        public MyResult RemoveRoleMenu(RemoveRoleMenu rrm)
        {
            return _menuService.RemoveRoleMenu(rrm.RoleId, rrm.MenuId).ToApiResult();
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
        public MyResult UpdateSort(MenuSortModel model)
        {
            var result = _menuService.UpdateSort(model);
            return result ? MyResult.Success() : MyResult.Fail("Failed to adjust menu order.");
        }
    }
}