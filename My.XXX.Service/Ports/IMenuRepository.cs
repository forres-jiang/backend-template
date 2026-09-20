using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service.Ports;
public interface IMenuRepository
{
    MenuBase Get(int id);
    int Insert(SaveMenu menu, string culture, string userId);
    int Remove(List<int> ids, string userId);
    int Update(EditMenu menu, string culture, string userId);
    bool SetRoleMenus(Guid roleId, List<int> menuIds, RoleMenuChange change, string userId);
    BatchWriteSummary ReplaceRoleActions(Guid roleId, List<int> menuIds, string userId);
    bool Move(MenuSortModel model, string userId);
    Paged<MenuBase> Search(QueryMenu query);
    List<MenuBase> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null);
    List<MenuBase> GetRoleMenuByRoles(List<Guid> roleIds);
    Task<long> GetPermissionRevisionAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetPermissionPathsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default);
}
