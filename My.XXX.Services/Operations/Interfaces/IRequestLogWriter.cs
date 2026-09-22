using My.XXX.Contracts.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Services.Operations.Interfaces;

/// <summary>尽力而为的运维日志记录。并非持久化的业务审计契约。</summary>
public interface IRequestLogWriter
{
    Task WriteAsync(MetricsInfo record);
}
