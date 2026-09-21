using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Services.Common;

public static class MenuUpdateFields
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    { "Description", "Icon", "Url", "Component", "ControllerName", "ActionName", "LinkTarget" };
    public static bool Valid(IEnumerable<string> fields) => fields == null || fields.All(f => f != null && Allowed.Contains(f));
}
