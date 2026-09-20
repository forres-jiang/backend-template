using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using My.XXX.Shared.Common;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace My.XXX.Infrastructure.Logging;

/// <summary>Ordinary telemetry must never replace the business outcome, even if a sink fails.</summary>
public sealed class RequestLogWriter(IOperationRepository operations, IOptionsMonitor<RequestLogOptions> options,
    ILogger<RequestLogWriter> logger) : IRequestLogWriter
{
    public async Task WriteAsync(MetricsInfo record)
    {
        var storage = record.IsException ? options.CurrentValue.ExceptionStorageType : options.CurrentValue.RequestLogStorageType;
        if (storage is StorageTypeEnum.SQL or StorageTypeEnum.TextAndSQL)
        {
            try { await operations.Save(record); }
            catch (Exception ex)
            {
                logger.LogError("Failed to persist request log {RequestId}: {ExceptionType}", record.RequestId, ex.GetType().Name);
            }
        }
        // Legacy email modes fall back to text; the removed mail module is not reinstated.
        if (storage != StorageTypeEnum.SQL)
        {
            if (record.IsException) logger.LogError("Request failed: {Metadata}", JsonConvert.SerializeObject(record));
            else logger.LogInformation("Request {RequestId} completed in {ElapsedMs} ms", record.RequestId, record.TotalTime);
        }
    }
}
