
namespace My.XXX.Infrastructure.Logging;

public sealed class RequestLogOptions
{
    public int QueueCapacity { get; set; } = 1024;
    public int WriteTimeoutSeconds { get; set; } = 5;
    public StorageTypeEnum RequestLogStorageType { get; set; } = StorageTypeEnum.Text;
    public StorageTypeEnum ExceptionStorageType { get; set; } = StorageTypeEnum.Text;
}
