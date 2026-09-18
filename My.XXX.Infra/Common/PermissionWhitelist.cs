using System.Collections.Generic;

namespace My.XXX.Infra
{
    public class PermissionWhitelist
    {
        public List<string> Controllers { get; set; }
        public List<string> Actions { get; set; }
    }
}