using My.XXX.Infra.Common;

namespace My.XXX.Infra
{
    public class AppConfig
    {
        public bool EnableRequestLog { get; set; }
        public StorageTypeEnum RequestLogStorageType { get; set; }
        public StorageTypeEnum ExceptionStorageType { get; set; }
        public PermissionDataCache PermissionDataCache { get; set; }
        public ExceptionEmailInfo ExceptionEmail { get; set; }
    }

    public class ExceptionEmailInfo
    {
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
    }

    public class ConnectionStrings
    {
        public string Default { get; set; }
        public string MailMaster { get; set; }
    }
}