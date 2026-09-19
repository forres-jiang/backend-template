using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace My.XXX.APIs.Common
{
    public class AppMetricsAsync : IAsyncActionFilter
    {
        private readonly IOperationService _operationService;
        private readonly ILogger<AppMetricsAsync> _logger;
        private readonly AppCenterConfig _appCenter;
        private readonly AppConfig _appSettings;
        private readonly IWebHelper _webHelper;

        public AppMetricsAsync(
            IOptionsMonitor<AppCenterConfig> appCenter,
            IOptionsMonitor<AppConfig> settings,
            IOperationService operationService,
            ILogger<AppMetricsAsync> logger,
            IWebHelper webHelper)
        {
            _appSettings = settings.CurrentValue;
            _operationService = operationService;
            _appCenter = appCenter.CurrentValue;
            _webHelper = webHelper;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_appSettings.EnableRequestLog)
            {
                await next();
                return;
            }

            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            {
                await next();
                return;
            }

            if (!IsMetrics(context.ActionDescriptor))
            {
                await next();
                return;
            }

            var record = new MetricsInfo()
            {
                HostName = Dns.GetHostName(),
                CreateTime = DateTime.Now,
                ControllerName = descriptor.ControllerName,
                ActionName = descriptor.ActionName,
                Inputs = context.ActionArguments,
                Url = GetRawUrl(context.HttpContext.Request)
            };

            Stopwatch watch = new();

            watch.Start();
            await next();
            watch.Stop();

            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var user = context.HttpContext.User;
                record.UserId = user.Claims.First(p => p.Type == ClaimTypes.NameIdentifier).Value;
                record.UserName = user.Identity.Name;
            }

            record.AppCode = _appCenter.AppCode;
            record.TotalTime = watch.ElapsedMilliseconds;
            record.ClientIP = _webHelper.GetCurrentIpAddress();
            record.RequestType = context.HttpContext.Request.Method;

            if (context.Result is FileStreamResult)
            {
                var fsr = context.Result as FileStreamResult;
                record.ReturnValue = new { fsr.ContentType, fsr.FileDownloadName };
            }
            else
            {
                var objectResult = context.Result as ObjectResult;
                var declaredType = objectResult?.DeclaredType;
                //由于查询数据列表返回结果较多将不保存到数据库
                if (declaredType != null && declaredType.Name != typeof(Paged<>).Name)
                {
                    record.ReturnValue = context.Result;
                }
            }

            if (_appSettings.RequestLogStorageType == StorageTypeEnum.Text)
            {
                _logger.LogCritical("RequestLogs: {log}", JsonConvert.SerializeObject(record));
            }
            else if (_appSettings.RequestLogStorageType == StorageTypeEnum.SQL)
            {
                await _operationService.Save(record);
            }
        }

        /// <summary>
        /// 是否忽略当前过滤器
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static bool IsMetrics(ActionDescriptor context)
        {
            var emList = context.EndpointMetadata.ToList();
            string attributeName = typeof(IgnoreMetrics).ToString();
            var IsVerify = emList.Where(m => m.ToString() == attributeName).FirstOrDefault();
            return IsVerify == null;
        }

        /// <summary>
        /// 获取Url
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static string GetRawUrl(HttpRequest request)
        {
            var rawUrl = request.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
            if (string.IsNullOrEmpty(rawUrl))
            {
                rawUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
            }
            return HttpUtility.UrlDecode(rawUrl);
        }
    }

    public class IgnoreMetrics : ActionFilterAttribute
    { }
}