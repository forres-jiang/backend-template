using System.Threading;
using System.Threading.Tasks;
using My.XXX.Service.Models;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.Service.Ports;

public interface IMenuReadRepository
{
    Task<MenuState> Get(int id, CancellationToken cancellationToken = default);
    Task<Paged<MenuState>> Search(MenuSearch query, CancellationToken cancellationToken = default);
    Task<List<MenuState>> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null, CancellationToken cancellationToken = default);
    Task<List<MenuState>> GetRoleMenuByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default);
}
