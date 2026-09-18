using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using My.XXX.Infra;
using My.XXX.Infra.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IOperationService _operations;
    private readonly IMailService _mail;
    private readonly AppConfig _config;
    private readonly AppCenterConfig _appCenter;

    public ExceptionHandlingMiddleware(IOptionsMonitor<AppCenterConfig> appCenter,
        ILogger<ExceptionHandlingMiddleware> logger, IOptionsMonitor<AppConfig> config,
        IOperationService operationService, IMailService mailService)
    {
        _logger = logger;
        _operations = operationService;
        _mail = mailService;
        _config = config.CurrentValue;
        _appCenter = appCenter.CurrentValue;
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
                    state = "0", message = "An internal error occurred.", requestId
                }, context.RequestAborted);
            }
        }
        finally
        {
            timer.Stop();
            if (failure != null || (_config.EnableRequestLog &&
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
                AppCode = _appCenter.AppCode,
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
            var storage = exception == null ? _config.RequestLogStorageType : _config.ExceptionStorageType;
            if (storage == StorageTypeEnum.SQL || storage == StorageTypeEnum.TextAndSQL)
                await _operations.Save(record);
            if (exception != null && (storage == StorageTypeEnum.Email || storage == StorageTypeEnum.TextAndEmail))
                _mail.SendEmail(new Mail
                {
                    MFROM = _config.ExceptionEmail?.MailFrom,
                    MTO = _config.ExceptionEmail?.MailTo,
                    SENDDATE = DateTime.UtcNow,
                    SUBJECT = $"{_appCenter.AppCode} exception",
                    CONTENT = $"Request {requestId} failed. Check the application logs."
                });
            if (exception == null)
                _logger.LogInformation("Request {RequestId} completed in {ElapsedMs} ms with status {StatusCode}",
                    requestId, elapsed, context.Response.StatusCode);
            else
                _logger.LogError("Request failed: {Metadata}", JsonConvert.SerializeObject(record));
        }
        catch (Exception loggingException)
        {
            // Logging failures cannot change the business response or replace the original exception.
            _logger.LogError("Failed to persist request log {RequestId}: {ExceptionType}",
                requestId, loggingException.GetType().Name);
        }
    }
}