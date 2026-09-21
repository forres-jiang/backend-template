using System.Collections.Generic;
namespace My.XXX.Services.Common;

/// <summary>Stable security identifiers. Navigation labels, routes and CLR names are not permissions.</summary>
public static class PermissionCodes
{
    public const string MenuAdd = "menu.add", MenuRemove = "menu.remove", MenuEdit = "menu.edit", MenuGet = "menu.get",
        MenuList = "menu.list", MenuSearch = "menu.search", MenuRoleMenu = "menu.role-menu",
        MenuRoleMenus = "menu.role-menus", MenuRoleMenuChecked = "menu.role-menu-checked",
        MenuRemoveRoleMenu = "menu.remove-role-menu", MenuTree = "menu.tree", MenuDisplayTree = "menu.display-tree",
        MenuTreeByRoleId = "menu.tree-by-role-id", MenuTreeByRoleIds = "menu.tree-by-role-ids",
        MenuUpdateSort = "menu.update-sort", OperationList = "operation.list",
        PermissionsRead = "permissions.read", PermissionsWrite = "permissions.write";
    public static IReadOnlyList<string> All { get; } = System.Array.AsReadOnly(new[] {
        MenuAdd, MenuRemove, MenuEdit, MenuGet, MenuList, MenuSearch, MenuRoleMenu, MenuRoleMenus, MenuRoleMenuChecked,
        MenuRemoveRoleMenu, MenuTree, MenuDisplayTree, MenuTreeByRoleId, MenuTreeByRoleIds, MenuUpdateSort,
        OperationList, PermissionsRead, PermissionsWrite });
}
