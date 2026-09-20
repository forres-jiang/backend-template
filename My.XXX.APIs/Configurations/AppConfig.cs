using My.XXX.Shared.Common;

namespace My.XXX.Shared
{
    public class AppConfig
    {
        public bool EnableRequestLog { get; set; }
        public StorageTypeEnum RequestLogStorageType { get; set; }
        public StorageTypeEnum ExceptionStorageType { get; set; }
        public PermissionDataCache PermissionDataCache { get; set; }
    }

    public class ConnectionStrings
    {
        public string Default { get; set; }
    }
}