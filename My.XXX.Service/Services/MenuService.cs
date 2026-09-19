using FluentResults;
using My.XXX.Persistence.Interfaces;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service
{
    public class MenuService : IMenuService, IScopeDependency
    {
        private readonly ICurrentRequest _currentRequest;
        private readonly IMenuRepository _menuRepository;
        private readonly IUserService _userService;
        private readonly ApplicationMapper _mapper;
        private readonly string _cultureName;

        public MenuService(
            ICurrentRequest currentRequest,
            IMenuRepository menuRepository,
            IUserService userService,
            ApplicationMapper mapper)
        {
            _menuRepository = menuRepository;
            _userService = userService;
            _mapper = mapper;
            _currentRequest = currentRequest;
            _cultureName = currentRequest.CultureName;
        }

        /// <summary>
        /// 添加菜单或按钮
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public Result Add(SaveMenu menu)
        {
            if (string.IsNullOrWhiteSpace(menu.DisplayName))
            {
                return Result.Fail("Menu name cannot be empty.");
            }

            if (menu.IsAction.Value)
            {
                var parentNode = _menuRepository.Get(menu.ParentId);

                if (parentNode == null)
                {
                    return Result.Fail("Parent node not found.");
                }

                if (parentNode.IsAction)
                {
                    return Result.Fail("A button node cannot add child nodes.");
                }
            }

            if (!string.IsNullOrWhiteSpace(menu.ActionName))
            {
                menu.ActionName = menu.ActionName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(menu.ControllerName))
            {
                menu.ControllerName = menu.ControllerName.Trim();
            }

            var user = _userService.CurrentUser;
            var model = _mapper.ToMenu(menu);
            model.DisplayNames = CreateDisplayNames(menu.DisplayName);
            model.CreatedBy = user.UserId;
            model.CreatedTime = DateTime.Now;
            model.UpdatedBy = user.UserId;
            model.UpdatedTime = DateTime.Now;

            var value = _menuRepository.Insert(model);
            return value > 0 ? Result.Ok() : Result.Fail("Add data failed.");
        }

        /// <summary>
        /// 删除菜单或按钮
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Result Remove(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return Result.Fail("Id cannot be empty");
            }

            var value = _menuRepository.Remove(ids, _userService.CurrentUser.UserId);

            return value > 0 ? Result.Ok() : Result.Fail("Delete failed.");
        }

        /// <summary>
        /// 更新菜单或按钮
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public Result Update(EditMenu menu)
        {
            if (menu.Id <= 0) return Result.Fail("MenuId invalid.");
            string displayNames = null;
            if (!string.IsNullOrWhiteSpace(menu.DisplayName))
            {
                var oldMenu = _menuRepository.Get(menu.Id);
                if (oldMenu == null) return Result.Fail("Update failed.");
                displayNames = BuildDisplayNames(oldMenu.DisplayNames, menu.DisplayName);
            }
            return _menuRepository.Update(menu, displayNames, _userService.CurrentUser.UserId) > 0
                ? Result.Ok() : Result.Fail("Update failed.");
        }

        /// <summary>
        /// 根据Id获取菜单或按钮
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public MenuBaseDto Get(int menuId)
        {
            var menu = _menuRepository.Get(menuId);
            if (menu != null)
            {
                var menuDto = _mapper.ToMenuBaseDto(menu);
                menuDto.DisplayName = GetMenuNameByCulture(menu.DisplayNames);
                return menuDto;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 角色菜单关联
        /// </summary>
        /// <param name="roleId">角色Id</param>
        /// <param name="menuIds">多个菜单Id</param>
        /// <param name="isFull">是否保存整个树选中的节点</param>
        /// <returns></returns>
        public Result RoleMenus(Guid roleId, List<int> menuIds, bool isFull)
        {
            if (roleId == Guid.Empty || menuIds == null || menuIds.Any(id => id <= 0))
                return Result.Fail("Invalid role or menu selection.");
            var inputMenuIds = menuIds.Distinct().OrderBy(m => m).ToList();
            var query = _menuRepository.GetRoleMenu(roleId).AsEnumerable();
            if (!isFull)
            {
                query = query.Where(m => menuIds.Contains(m.MenuId));
            }

            var dbMenuIds = query.Select(m => m.MenuId).Distinct().OrderBy(m => m).ToList();
            if (dbMenuIds.Count > 0)
            {
                var isEqual = inputMenuIds.SequenceEqual(dbMenuIds);
                if (isEqual)
                {
                    return Result.Ok();
                }
            }

            var saveIds = inputMenuIds.Except(dbMenuIds).ToList();
            List<RoleMenu> roleMenuModel = new();
            foreach (var item in saveIds)
            {
                roleMenuModel.Add(new RoleMenu
                {
                    CreatedBy = _userService.CurrentUser.UserId,
                    CreatedTime = DateTime.Now,
                    IsDeleted = false,
                    MenuId = item,
                    RoleId = roleId,
                });
            }

            var removeIds = dbMenuIds.Except(inputMenuIds).ToList();
            return Result.OkIf(_menuRepository.SaveRoleChanges(roleId, removeIds, roleMenuModel, _userService.CurrentUser.UserId), "Save failed.");
        }

        public Result RoleMenuRelation(InputRoleMenu input)
        {
            if (input.RoleId == Guid.Empty || !input.Checked.HasValue || input.MenuId <= 0)
            {
                return Result.Fail("Save failed.");
            }

            var rm = _menuRepository.GetRoleMenu(input.RoleId)
                .Where(m => m.RoleId == input.RoleId && m.MenuId == input.MenuId)
                .FirstOrDefault();

            if (input.Checked.Value)
            {
                if (null != rm)
                {
                    return Result.Ok();
                }

                return Result.OkIf(_menuRepository.AddRoleMenu(new Persistence.PersistantObjects.RoleMenu
                {
                    CreatedBy = _userService.CurrentUser.UserId,
                    CreatedTime = DateTime.Now,
                    RoleId = input.RoleId,
                    MenuId = input.MenuId,
                }), "Save failed.");
            }
            else
            {
                if (null == rm)
                {
                    return Result.Ok();
                }
                else
                {
                    return Result.OkIf(_menuRepository.DeleteRoleMenu(new Persistence.PersistantObjects.RoleMenu
                    {
                        UpdatedBy = _userService.CurrentUser.UserId,
                        RoleId = input.RoleId,
                        MenuId = input.MenuId
                    }), "Save failed.");
                }
            }
        }

        public Result RoleMenusRelation(InputRoleMenus input)
        {
            if (input.RoleId == Guid.Empty || !input.Checked.HasValue || input.MenuIds == null)
                return Result.Fail("Invalid role or menu selection.");
            if (input.Checked.Value)
            {
                return RoleMenus(input.RoleId, input.MenuIds, false);
            }

            var list = _menuRepository.GetRoleMenu(input.RoleId)
                .Where(m => m.RoleId == input.RoleId && input.MenuIds.Contains(m.MenuId))
                .Select(m => m.MenuId)
                .Distinct()
                .ToList();

            if (list.Count <= 0)
            {
                return Result.Ok();
            }
            return Result.OkIf(_menuRepository.RemoveRoleMenu(new List<Guid> { input.RoleId },
                  list, _userService.CurrentUser.UserId), "Save failed.");
        }

        /// <summary>
        /// 删除角色菜单关联
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public Result RemoveRoleMenu(Guid roleId, int menuId)
        {
            var result = _menuRepository.RemoveRoleMenu(new List<Guid> { roleId },
                new List<int> { menuId },
                _userService.CurrentUser.UserId);

            return result ? Result.Ok() : Result.Fail("Remove failed.");
        }

        /// <summary>
        /// 角色菜单按钮关联
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Result<BatchWriteSummary> RoleMenuAction(RoleMenuActionModel model)
        {
            if (model.RoleId == Guid.Empty) return Result.Fail<BatchWriteSummary>("RoleId invalid.");
            if (model.Menus == null || model.Menus.Count == 0 || model.Menus.Any(menu =>
                menu == null || menu.MenuId <= 0 || (menu.ActionIds != null && menu.ActionIds.Any(id => id <= 0))))
                return Result.Fail<BatchWriteSummary>("MenuId invalid.");
            var relations = RoleMenuRelations.Build(model, _userService.CurrentUser.UserId, DateTime.UtcNow);
            var result = _menuRepository.ReplaceRoleActions(model.RoleId, relations, _userService.CurrentUser.UserId);
            return result.RowsCopied == relations.Count ? Result.Ok(result) : Result.Fail<BatchWriteSummary>("Save failed.");
        }

        /// <summary>
        /// 根据多个角色获取的菜单树
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<MenuDto> GetMenuByRoles(RoleMenuQuery query)
        {
            var dbMenuAction = _menuRepository.GetRoleMenuByRoles(query.RoleIds)
                .Where(m => m.IsDisplay).ToList();

            var dtoList = _mapper.ToMenuDtos(dbMenuAction);

            SetMenuLanguage(dtoList);

            var dbMenus = dtoList.Where(m => !m.IsAction).ToList();

            foreach (var item in dbMenus)
            {
                var actions = dbMenuAction.Where(m => m.ParentId == item.Id && m.IsAction).ToList();
                if (actions != null && actions.Count > 0)
                {
                    item.Actions = _mapper.ToMenuDtos(actions);
                }
            }

            var tree = BulidMenusTree(dbMenus, new List<MenuDto>(), 0);
            return tree;
        }

        /// <summary>
        /// 根据多个角色获取菜单树，并且选中已经配置的节点
        /// </summary>
        /// <param name="roleIds">角色ID</param>
        /// <returns></returns>
        public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> roleIds)
        {
            var menus = _menuRepository.GetMenus();
            var list = _mapper.ToMenuDtos(menus);

            SetMenuLanguage(list);

            var roleMenus = _menuRepository.GetRoleMenuByRoles(roleIds).Select(m => m.Id).Distinct().ToList();

            foreach (var item in roleMenus)
            {
                var m = list.Where(m => m.Id == item).FirstOrDefault();
                if (m != null)
                {
                    m.Checked = true;
                }
            }

            var tree = BulidMenusTree(list, new List<MenuDto>(), 0);
            return tree;
        }

        /// <summary>
        /// 获取所有菜单
        /// </summary>
        /// <returns></returns>
        public Result<List<MenuBase>> GetMenus()
        {
            var menus = _menuRepository.GetMenus().ToList();
            return Result.Ok(_mapper.ToMenuBases(menus));
        }

        /// <summary>
        /// 获取所有菜单，以树形结构显示
        /// </summary>
        /// <returns></returns>
        public List<MenuDto> GetTreeMenus(bool? isDisplay)
        {
            var list = _mapper.ToMenuDtos(_menuRepository.GetMenus(isDisplay: isDisplay));
            SetMenuLanguage(list);
            return BulidMenusTree(list, new List<MenuDto>(), 0);
        }

        public Paged<MenuBaseDto> GetMenus(QueryMenu query)
        {
            var page = _menuRepository.Search(query);
            SetDisplayName(page.List);
            return Paged<MenuBaseDto>.Create(_mapper.ToMenuBaseDtos(page.List), page.Total);
        }

        public Paged<MenuSearchPickerDto> SearchMenus(QueryMenu query)
        {
            query.IsAction = false;
            query.IsDisplay = true;

            var result = GetMenus(query);
            var dtoList = _mapper.ToMenuSearchPickersFromBase(result.List)
                .OrderBy(m => m.ParentId).ThenBy(m => m.Number).ToList();

            SetMenuLanguage(dtoList);
            return Paged<MenuSearchPickerDto>.Create(dtoList, result.Total);
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public void SetMenuLanguage<T>(List<T> list) where T : MenuBase
        {
            foreach (var item in list)
            {
                var displayName = GetMenuNameByCulture(item.DisplayNames);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    item.DisplayName = displayName;
                }
            }
        }

        /// <summary>
        ///递归构建菜单树结构
        /// </summary>
        /// <param name="treeNodes"></param>
        /// <param name="resps"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public List<MenuDto> BulidMenusTree(List<MenuDto> treeNodes, List<MenuDto> resps, int parentId)
        {
            resps = new List<MenuDto>();
            var nodes = treeNodes.Where(c => c.ParentId == parentId)
                .OrderBy(m => m.Number)
                .ThenBy(m => m.UpdatedTime)
                .ToList();

            foreach (var item in nodes)
            {
                item.Children = BulidMenusTree(treeNodes, resps, item.Id);
                resps.Add(item);
            }
            return resps;
        }

        /// <summary>
        /// 更新菜单排序
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Result UpdateSort(MenuSortModel model)
        {
            if (model.PrevId == model.NextId)
            {
                return Result.Fail("Save failed.");
            }

            var ids = new List<int>() { model.PrevId, model.CurrentId, model.NextId };
            var menus = _menuRepository.GetMenus(ids: ids);

            var prevNode = menus.Where(m => m.Id == model.PrevId).FirstOrDefault();
            var currentNode = menus.Where(m => m.Id == model.CurrentId).FirstOrDefault();
            var nextNode = menus.Where(m => m.Id == model.NextId).FirstOrDefault();

            int parentId;
            bool isPrev = false;
            bool parentLevel = true;

            if (model.PrevId == 0)
            {
                if (null == currentNode || null == nextNode)
                {
                    return Result.Fail("Save failed.");
                }
                parentId = nextNode.ParentId;
                parentLevel = nextNode.ParentId == currentNode.ParentId;
            }
            else
            {
                if (null == currentNode || null == prevNode)
                {
                    return Result.Fail("Save failed.");
                }
                isPrev = true;
                parentId = prevNode.ParentId;
                parentLevel = prevNode.ParentId == currentNode.ParentId;
            }

            var query = _menuRepository.GetMenus(parentId: parentId).AsEnumerable();
            if (parentLevel)
            {
                query = query.Where(m => m.Id != currentNode.Id);
            }
            var allList = query.OrderBy(m => m.Number).ThenBy(m => m.UpdatedTime).ToList();

            if (isPrev)
            {
                var index = allList.FindIndex(m => m.Id == model.PrevId);
                allList.Insert(index + 1, new Menus
                {
                    Id = model.CurrentId,
                    ParentId = prevNode.ParentId
                });
            }
            else
            {
                var index = allList.FindIndex(m => m.Id == model.NextId);
                allList.Insert(index, new Menus
                {
                    Id = model.CurrentId,
                    ParentId = nextNode.ParentId
                });
            }

            var user = _userService.CurrentUser;
            return Result.OkIf(_menuRepository.SaveOrder(allList, user.UserId), "Save failed.");
        }

        /// <summary>
        /// 构建菜单多语言值对象
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        private string BuildDisplayNames(string oldName, string newName)
        {
            var cultureName = _currentRequest.CultureName;
            var oldItems = JsonConvert.DeserializeObject<Dictionary<string, string>>(oldName);
            var list = new Dictionary<string, string>();

            foreach (var item in oldItems)
            {
                if (item.Key.Equals(cultureName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                list.Add(item.Key, item.Value);
            }

            list.Add(cultureName, newName);
            return JsonConvert.SerializeObject(list);
        }

        /// <summary>
        /// 根据Culture获取菜单名称
        /// </summary>
        /// <param name="menuName"></param>
        /// <returns></returns>
        private string GetMenuNameByCulture(string menuName)
        {
            var list = JsonConvert.DeserializeObject<Dictionary<string, string>>(menuName);
            foreach (var item in list)
            {
                if (item.Key.Equals(_cultureName, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }

            return string.Empty;
        }

        private string CreateDisplayNames(string menuName)
        {
            var cultureName = _currentRequest.CultureName;
            return JsonConvert.SerializeObject(new Dictionary<string, string> { { cultureName, menuName.Trim() } });
        }

        private void SetDisplayName(List<Menus> menuList)
        {
            var cultureName = _currentRequest.CultureName;

            foreach (var menu in menuList)
            {
                if (string.IsNullOrWhiteSpace(menu.DisplayNames))
                {
                    continue;
                }

                var names = menu.DisplayNames.Trim();
                if (names.StartsWith('{') && names.EndsWith('}') && names.IndexOf(':') > -1)
                {
                    var list = JsonConvert.DeserializeObject<Dictionary<string, string>>(menu.DisplayNames);
                    foreach (var item in list)
                    {
                        if (item.Key.Equals(cultureName, StringComparison.OrdinalIgnoreCase))
                        {
                            menu.DisplayName = item.Value;
                        }
                    }
                }
            }
        }
    }
}
