using My.XXX.Services.Menus.Models;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Services.Menus.Policies;

public static class MenuTree
{
    public static List<MenuNode> Build(List<MenuNode> menus)
    {
        var children = menus.ToLookup(m => m.Menu.ParentId);
        var visited = new HashSet<int>();
        List<MenuNode> Visit(int parentId)
        {
            var result = new List<MenuNode>();
            foreach (var menu in children[parentId].OrderBy(m => m.Menu.Number).ThenBy(m => m.Menu.UpdatedTime))
            {
                if (!visited.Add(menu.Menu.Id)) continue;
                menu.Children = Visit(menu.Menu.Id);
                result.Add(menu);
            }
            return result;
        }
        return Visit(0);
    }
}
