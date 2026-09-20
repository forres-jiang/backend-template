using My.XXX.Service.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces;

/// <summary>Best-effort operational logging. Not a durable business audit contract.</summary>
public interface IRequestLogWriter
{
    Task WriteAsync(MetricsInfo record);
}
