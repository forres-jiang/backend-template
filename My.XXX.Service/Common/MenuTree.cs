using My.XXX.Service.DTOs;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Service.Common;

public static class MenuTree
{
    public static List<MenuDto> Build(List<MenuDto> menus)
    {
        var children = menus.ToLookup(m => m.ParentId);
        var visited = new HashSet<int>();
        List<MenuDto> Visit(int parentId)
        {
            var result = new List<MenuDto>();
            foreach (var menu in children[parentId].OrderBy(m => m.Number).ThenBy(m => m.UpdatedTime))
            {
                if (!visited.Add(menu.Id)) continue;
                menu.Children = Visit(menu.Id);
                result.Add(menu);
            }
            return result;
        }
        return Visit(0);
    }
}
