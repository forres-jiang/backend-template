using My.XXX.Service.Models;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.Service.Ports;

public interface IMenuReadRepository
{
    MenuState Get(int id);
    Paged<MenuState> Search(MenuSearch query);
    List<MenuState> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null);
    List<MenuState> GetRoleMenuByRoles(List<Guid> roleIds);
}
