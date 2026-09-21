using My.XXX.Contracts.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Services.Interfaces;

/// <summary>Best-effort operational logging. Not a durable business audit contract.</summary>
public interface IRequestLogWriter
{
    Task WriteAsync(MetricsInfo record);
}
