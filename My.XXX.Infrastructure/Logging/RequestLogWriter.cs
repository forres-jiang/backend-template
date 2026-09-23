using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Operations.Interfaces;
using My.XXX.Services.Operations.Ports;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace My.XXX.Infrastructure.Logging;

/// <summary>普通遥测日志绝不能取代业务结果，即使某个日志接收端（sink）发生故障也是如此。</summary>
public sealed class RequestLogWriter(IOperationRepository operations, IOptionsMonitor<RequestLogOptions> options,
    ILogger<RequestLogWriter> logger) : IRequestLogWriter
{
    public Task WriteAsync(MetricsInfo record) => WriteAsync(record, CancellationToken.None);
    public async Task WriteAsync(MetricsInfo record, CancellationToken cancellationToken)
    {
        var storage = record.IsException ? options.CurrentValue.ExceptionStorageType : options.CurrentValue.RequestLogStorageType;
        if (storage is StorageTypeEnum.SQL or StorageTypeEnum.TextAndSQL)
        {
            try { await operations.Save(record, cancellationToken); }
            catch (Exception ex)
            {
                logger.LogError("Failed to persist request log {RequestId}: {ExceptionType}", record.RequestId, ex.GetType().Name);
            }
        }
        // 旧版邮件模式回退为文本记录；不会恢复已移除的邮件模块。
        if (storage != StorageTypeEnum.SQL)
        {
            if (record.IsException) logger.LogError("Request failed: {Metadata}", JsonConvert.SerializeObject(record));
            else logger.LogInformation("Request {RequestId} trace {TraceId} completed in {ElapsedMs} ms", record.RequestId, record.TraceId, record.TotalTime);
        }
    }
}
