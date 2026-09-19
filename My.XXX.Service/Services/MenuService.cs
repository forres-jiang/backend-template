using FluentResults;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Data;
using My.XXX.Data.Interfaces;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using My.XXX.Infra.Common;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service
{
    public class MenuService : IMenuService, IScopeDependency
    {
        private readonly ICurrentRequest _currentRequest;
        private readonly IPermissionCache _permissionCache;
        private readonly IMenuRepository _menuRepository;
        private readonly ILogger<MenuService> _logger;
        private readonly IUserService _userService;
        private readonly DBContext _dbContext;
        private readonly JwtConfig _jwtConfig;
        private readonly AppConfig _appConfig;
        private readonly ApplicationMapper _mapper;
        private readonly string _cultureName;

        public MenuService(
            ICurrentRequest currentRequest,
            IPermissionCache permissionCache,
            IOptionsMonitor<AppConfig> appConfig,
            IOptionsMonitor<JwtConfig> jwtConfig,
            IMenuRepository menuRepository,
            ILogger<MenuService> logger,
            IUserService userService,
            DBContext dbContext,
            ApplicationMapper mapper)
        {
            _appConfig = appConfig.CurrentValue;
            _jwtConfig = jwtConfig.CurrentValue;
            _menuRepository = menuRepository;
            _userService = userService;
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _currentRequest = currentRequest;
            _permissionCache = permissionCache;
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
                var parentNode = _dbContext.Menus
                    .Where(m => m.Id == menu.ParentId && !m.IsDeleted).FirstOrDefault();

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

            var value = _dbContext.Insert(model);
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

            var value = _dbContext.Menus.Where(m => !m.IsDeleted && ids.Contains(m.Id))
                .Set(m => m.IsDeleted, true)
                .Set(m => m.UpdatedTime, DateTime.Now)
                .Set(m => m.UpdatedBy, _userService.CurrentUser.UserId)
                .Update();

            return value > 0 ? Result.Ok() : Result.Fail("Delete failed.");
        }

        /// <summary>
        /// 更新菜单或按钮
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public Result Update(EditMenu menu)
        {
            if (menu.Id <= 0)
            {
                return Result.Fail("MenuId invalid.");
            }

            var statement = _dbContext.Menus.Where(m => m.Id == menu.Id && !m.IsDeleted)
                .Set(m => m.UpdatedTime, DateTime.Now)
                .Set(m => m.UpdatedBy, _userService.CurrentUser.UserId);
            /*
            if (!string.IsNullOrEmpty(menu.Description))
                statement = statement.Set(m => m.Description, menu.Description);

            if (!string.IsNullOrEmpty(menu.Icon))
                statement = statement.Set(m => m.Icon, menu.Icon);

            if (!string.IsNullOrEmpty(menu.Url))
                statement = statement.Set(m => m.Url, menu.Url);

            if (!string.IsNullOrEmpty(menu.Component))
                statement = statement.Set(m => m.Component, menu.Component);

            if (!string.IsNullOrEmpty(menu.RequestParameter))
                statement = statement.Set(m => m.RequestParameter, menu.RequestParameter);

            if (!string.IsNullOrEmpty(menu.ControllerName))
                statement = statement.Set(m => m.ControllerName, menu.ControllerName);

            if (!string.IsNullOrEmpty(menu.ActionName))
                statement = statement.Set(m => m.ActionName, menu.ActionName);

            if (!string.IsNullOrEmpty(menu.LinkTarget))
                statement = statement.Set(m => m.LinkTarget, menu.LinkTarget);
            */

            var updateHelper = new UpdateHelper<Menus>(statement);
            statement = updateHelper.GetCondition(menu);

            if (!string.IsNullOrWhiteSpace(menu.DisplayName))
            {
                var oldMenu = _dbContext.Menus.Where(m => m.Id == menu.Id).FirstOrDefault();
                var displayNames = BuildDisplayNames(oldMenu.DisplayNames, menu.DisplayName);
                statement = statement.Set(m => m.DisplayNames, displayNames);
            }

            int value = statement.Update();

            return value > 0 ? Result.Ok() : Result.Fail("Update failed.");
        }

        /// <summary>
        /// 根据Id获取菜单或按钮
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public MenuBaseDto Get(int menuId)
        {
            var menu = _menuRepository.GetMenus().Where(m => m.Id == menuId).FirstOrDefault();
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
        public bool RoleMenus(Guid roleId, List<int> menuIds, bool isFull)
        {
            var inputMenuIds = menuIds.Distinct().OrderBy(m => m).ToList();
            var query = _menuRepository.GetRoleMenu().Where(m => m.RoleId == roleId);
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
                    return true;
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

            _dbContext.BeginTransaction();
            try
            {
                if (dbMenuIds.Count > 0)
                {
                    var removeIds = dbMenuIds.Except(inputMenuIds).ToList();
                    if (removeIds.Count > 0)
                    {
                        var value = _dbContext.RoleMenu
                            .Where(m => m.RoleId == roleId && !m.IsDeleted && removeIds.Contains(m.MenuId))
                            .Set(m => m.IsDeleted, true)
                            .Set(m => m.UpdatedBy, _userService.CurrentUser.UserId)
                            .Set(m => m.UpdatedTime, DateTime.Now)
                            .Update();

                        if (value <= 0)
                        {
                            _dbContext.RollbackTransaction();
                            return false;
                        }
                    }
                }

                BulkCopyRowsCopied result = null;
                if (roleMenuModel.Count > 0)
                {
                    result = _dbContext.BulkCopy(roleMenuModel);
                }

                if (result != null && result.RowsCopied != roleMenuModel.Count)
                {
                    _dbContext.RollbackTransaction();
                    return false;
                }

                _dbContext.CommitTransaction();
                return true;
            }
            catch (Exception ex)
            {
                _dbContext.RollbackTransaction();
                _logger.LogError("{Message}", ex.Message);
                return false;
            }
        }

        public bool RoleMenuRelation(InputRoleMenu input)
        {
            if (input.RoleId == Guid.Empty)
            {
                return false;
            }

            var rm = _menuRepository.GetRoleMenu()
                .Where(m => m.RoleId == input.RoleId && m.MenuId == input.MenuId)
                .FirstOrDefault();

            if (input.Checked.Value)
            {
                if (null != rm)
                {
                    return true;
                }

                return _menuRepository.AddRoleMenu(new Data.PersistantObjects.RoleMenu
                {
                    CreatedBy = _userService.CurrentUser.UserId,
                    CreatedTime = DateTime.Now,
                    RoleId = input.RoleId,
                    MenuId = input.MenuId,
                });
            }
            else
            {
                if (null == rm)
                {
                    return true;
                }
                else
                {
                    return _menuRepository.DeleteRoleMenu(new Data.PersistantObjects.RoleMenu
                    {
                        UpdatedBy = _userService.CurrentUser.UserId,
                        RoleId = input.RoleId,
                        MenuId = input.MenuId
                    });
                }
            }
        }

        public bool RoleMenusRelation(InputRoleMenus input)
        {
            if (input.Checked.Value)
            {
                return RoleMenus(input.RoleId, input.MenuIds, false);
            }

            var list = _menuRepository.GetRoleMenu()
                .Where(m => m.RoleId == input.RoleId && input.MenuIds.Contains(m.MenuId))
                .Select(m => m.MenuId)
                .Distinct()
                .ToList();

            if (list.Count <= 0)
            {
                return true;
            }
            return _menuRepository.RemoveRoleMenu(new List<Guid> { input.RoleId },
                  list, _userService.CurrentUser.UserId);
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
        public Result<BulkCopyRowsCopied> RoleMenuAction(RoleMenuActionModel model)
        {
            if (model.RoleId == Guid.Empty)
            {
                return Result.Fail<BulkCopyRowsCopied>("RoleId invalid.");
            }

            if (model.Menus == null || model.Menus.Count == 0)
            {
                return Result.Fail<BulkCopyRowsCopied>("MenuId invalid.");
            }

            _dbContext.BeginTransaction();
            try
            {
                _dbContext.RoleMenu.Where(m => m.RoleId == model.RoleId && !m.IsDeleted)
                    .Set(m => m.IsDeleted, true)
                    .Set(m => m.UpdatedTime, DateTime.Now)
                    .Set(m => m.UpdatedBy, _userService.CurrentUser.UserId)
                    .Update();

                var relations = RoleMenuRelations.Build(model, _userService.CurrentUser.UserId, DateTime.UtcNow);

                var result = _dbContext.BulkCopy(relations);
                if (result.RowsCopied != relations.Count)
                {
                    _dbContext.RollbackTransaction();
                    return Result.Fail<BulkCopyRowsCopied>("Save failed.");
                }

                _dbContext.CommitTransaction();
                return Result.Ok(result);
            }
            catch (Exception)
            {
                _dbContext.RollbackTransaction();
                return Result.Fail<BulkCopyRowsCopied>("Save failed.");
            }
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

        public List<string> GetRoleMenuPaths(List<Guid> roleIds)
        {
            var roleMenus = _menuRepository.GetRoleMenuByRoles(roleIds).ToList();
            var list = new List<string>();
            roleMenus.ForEach(m =>
            {
                if (!string.IsNullOrEmpty(m.ControllerName) && !string.IsNullOrEmpty(m.ActionName))
                {
                    list.Add(m.ControllerName + "/" + m.ActionName);
                }
                else
                {
                    if (!string.IsNullOrEmpty(m.Url))
                    {
                        list.Add(m.Url);
                    }
                }
            });
            return list;
        }

        public List<string> GetRoleMenuPaths(List<Guid> roleIds, string userId)
        {
            if (_appConfig.PermissionDataCache == PermissionDataCache.Redis)
            {
                if (_permissionCache.TryGet(userId, out var list))
                {
                    return list;
                }

                var paths = GetRoleMenuPaths(roleIds);
                _permissionCache.Set(userId, paths, TimeSpan.FromMinutes(_jwtConfig.ExpiryInMinutes));
                return paths;
            }
            else
            {
                return GetRoleMenuPaths(roleIds);
            }
        }

        /// <summary>
        /// 根据多个角色获取菜单树，并且选中已经配置的节点
        /// </summary>
        /// <param name="roleIds">角色ID</param>
        /// <returns></returns>
        public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> roleIds)
        {
            var menus = (from m in _dbContext.Menus where !m.IsDeleted select m).ToList();
            var list = _mapper.ToMenuDtos(menus);

            SetMenuLanguage(list);

            var roleMenus = (from rm in _dbContext.RoleMenu
                             join m in _dbContext.Menus on rm.MenuId equals m.Id
                             where !rm.IsDeleted && !m.IsDeleted && roleIds.Contains(rm.RoleId)
                             select rm.MenuId).Distinct().ToList();

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
        public Result<List<Menus>> GetMenus()
        {
            var menus = _menuRepository.GetMenus().ToList();
            return Result.Ok(menus);
        }

        /// <summary>
        /// 获取所有菜单，以树形结构显示
        /// </summary>
        /// <returns></returns>
        public List<MenuDto> GetTreeMenus(bool? isDisplay)
        {
            var allMenu = _menuRepository.GetMenus();

            if (isDisplay != null)
            {
                allMenu = allMenu.Where(m => m.IsDisplay == isDisplay);
            }

            var list = _mapper.ToMenuDtos(allMenu.ToList());
            SetMenuLanguage(list);
            var tree = BulidMenusTree(list, new List<MenuDto>(), 0);
            return tree;
        }

        public Paged<MenuBaseDto> GetMenus(QueryMenu query)
        {
            var menus = _menuRepository.GetMenus();

            if (query.IsAction != null)
            {
                menus = menus.Where(m => m.IsAction == query.IsAction);
            }

            if (!string.IsNullOrEmpty(query.DisplayName))
            {
                menus = menus.Where(m => m.DisplayNames.Contains(query.DisplayName));
            }

            if (query.ParentId != null)
            {
                menus = menus.Where(m => m.ParentId == query.ParentId.Value);
            }

            var total = menus.Count();

            var list = menus.OrderByDescending(m => m.CreatedTime)
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize).ToList();

            SetDisplayName(list);

            var dtoList = _mapper.ToMenuBaseDtos(list);
            return Paged<MenuBaseDto>.Create(dtoList, total);
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
        public bool UpdateSort(MenuSortModel model)
        {
            if (model.PrevId == model.NextId)
            {
                return false;
            }

            var ids = new List<int>() { model.PrevId, model.CurrentId, model.NextId };
            var menus = _menuRepository.GetMenus().Where(m => ids.Contains(m.Id)).ToList();

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
                    return false;
                }
                parentId = nextNode.ParentId;
                parentLevel = nextNode.ParentId == currentNode.ParentId;
            }
            else
            {
                if (null == currentNode || null == prevNode)
                {
                    return false;
                }
                isPrev = true;
                parentId = prevNode.ParentId;
                parentLevel = prevNode.ParentId == currentNode.ParentId;
            }

            var query = _menuRepository.GetMenus().Where(m => m.ParentId == parentId);
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
            var resultList = new List<int>();
            var dt = DateTime.Now;
            for (int i = 0; i < allList.Count; i++)
            {
                var value = _dbContext.Menus.Where(m => m.Id == allList[i].Id)
                      .Set(m => m.Number, i)
                      .Set(m => m.UpdatedTime, dt)
                      .Set(m => m.UpdatedBy, user.UserId)
                      .Set(m => m.ParentId, allList[i].ParentId).Update();
                resultList.Add(value);
            }

            return !resultList.Contains(0);
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
