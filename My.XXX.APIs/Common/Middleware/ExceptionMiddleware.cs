using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Shared;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IRequestLogWriter _logs;
    private readonly AppConfig _config;

    public ExceptionHandlingMiddleware(
        ILogger<ExceptionHandlingMiddleware> logger, IOptionsMonitor<AppConfig> config,
        IRequestLogWriter logs)
    {
        _logger = logger;
        _logs = logs;
        _config = config.CurrentValue;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var timer = Stopwatch.StartNew();
        var requestId = Guid.NewGuid();
        context.Items["request_id"] = requestId;
        Exception failure = null;
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            failure = ex;
            if (context.Response.HasStarted)
                context.Abort();
            else
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    state = "0",
                    message = "An internal error occurred.",
                    requestId
                }, context.RequestAborted);
            }
        }
        finally
        {
            timer.Stop();
            if (failure != null || (_config.EnableRequestLog &&
                context.GetEndpoint()?.Metadata.GetMetadata<IgnoreMetrics>() == null &&
                !context.Request.Path.StartsWithSegments("/healthy") &&
                !context.Request.Path.StartsWithSegments("/ready")))
                await TrySaveLogs(context, requestId, timer.ElapsedMilliseconds, failure);
        }
    }

    private async Task TrySaveLogs(HttpContext context, Guid requestId, long elapsed, Exception exception)
    {
        try
        {
            // Metadata only: never read request bodies, query strings, headers or response payloads.
            var record = new MetricsInfo
            {
                RequestId = requestId,
                HostName = Environment.MachineName,
                CreateTime = DateTime.UtcNow,
                ControllerName = context.Request.RouteValues["controller"]?.ToString() ?? "",
                ActionName = context.Request.RouteValues["action"]?.ToString() ?? "",
                RequestType = context.Request.Method,
                ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "",
                TotalTime = elapsed,
                UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = context.User.Identity?.Name,
                Message = exception?.GetType().Name ?? "",
                StackTrace = exception?.StackTrace ?? "",
                IsException = exception != null
            };
            await _logs.WriteAsync(record);
        }
        catch (Exception loggingException)
        {
            // Logging failures cannot change the business response or replace the original exception.
            _logger.LogError("Failed to persist request log {RequestId}: {ExceptionType}",
                requestId, loggingException.GetType().Name);
        }
    }
}
