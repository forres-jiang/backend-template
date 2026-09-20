using My.XXX.Shared.Common;

namespace My.XXX.Infrastructure.Logging;

public sealed class RequestLogOptions
{
    public StorageTypeEnum RequestLogStorageType { get; set; } = StorageTypeEnum.Text;
    public StorageTypeEnum ExceptionStorageType { get; set; } = StorageTypeEnum.Text;
}
