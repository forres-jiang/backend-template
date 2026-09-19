using System;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces;

public interface IPermissionQuery
{
    List<string> GetRoleMenuPaths(List<Guid> roleIds);
    List<string> GetRoleMenuPaths(List<Guid> roleIds, string userId);
}
