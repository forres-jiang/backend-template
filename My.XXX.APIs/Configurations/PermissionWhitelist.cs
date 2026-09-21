using System.Collections.Generic;

namespace My.XXX.APIs.Configurations
{
    public class PermissionWhitelist
    {
        public List<string> Codes { get; set; } = new();
        public List<string> Controllers { get; set; }
        public List<string> Actions { get; set; }
    }
}