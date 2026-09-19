using System.Collections.Generic;

namespace My.XXX.Shared
{
    public class PermissionWhitelist
    {
        public List<string> Controllers { get; set; }
        public List<string> Actions { get; set; }
    }
}