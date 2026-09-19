namespace My.XXX.Shared.Common
{
    /// <summary>
    /// 日志和异常存储类型
    /// </summary>
    public enum StorageTypeEnum
    {
        Text = 1,
        SQL = 2,
        Email = 3,
        TextAndSQL = 4,
        TextAndEmail = 5
    }

    /// <summary>
    ///
    /// </summary>
    public class CultureType
    {
        public static readonly string en_US = "en-US";
        public static readonly string zh_CN = "zh-CN";
        public static readonly string zh_TW = "zh-TW";
        public static readonly string zh_HK = "zh-HK";
    }

    /// <summary>
    ///
    /// </summary>
    public class PolicyType
    {
        public static readonly string Default = "default";
    }

    /// <summary>
    ///权限缓存
    /// </summary>
    public enum PermissionDataCache
    {
        None = 0,
        Redis = 1
    }
}