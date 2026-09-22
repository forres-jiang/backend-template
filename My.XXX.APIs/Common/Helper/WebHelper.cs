using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using My.XXX.APIs.Configurations;
using My.XXX.Shared;
using System;
using System.Linq;

namespace My.XXX.APIs.Common
{
    public interface IWebHelper
    {
        /// <summary>
        /// 如果存在，则获取 URL 引荐来源地址
        /// </summary>
        /// <returns>URL 引荐来源地址</returns>
        string GetUrlReferrer();

        /// <summary>
        /// 获取一个值，该值指示当前连接是否安全
        /// </summary>
        /// <returns>如果安全则为 true，否则为 false</returns>
        bool IsCurrentConnectionSecured();

        /// <summary>
        /// 获取存储主机位置
        /// </summary>
        /// <param name="useSsl">是否获取 SSL 安全 URL</param>
        /// <returns>存储主机位置</returns>
        string GetStoreHost(bool useSsl);

        /// <summary>
        /// 如果请求的资源属于典型的无需由 CMS 引擎处理的资源，则返回 true。
        /// </summary>
        /// <returns>如果请求目标是静态资源文件，则为 true。</returns>
        bool IsStaticResource();

        /// <summary>
        /// 按名称获取查询字符串值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="name">查询参数名称</param>
        /// <returns>查询字符串值</returns>
        T QueryString<T>(string name);

        /// <summary>
        /// 获取一个值，该值指示客户端是否正在被重定向到新位置
        /// </summary>
        bool IsRequestBeingRedirected { get; }

        /// <summary>
        /// 获取当前 HTTP 请求协议
        /// </summary>
        string GetCurrentRequestProtocol();

        /// <summary>
        /// 获取请求的原始路径和完整查询
        /// </summary>
        /// <param name="request">HTTP 请求</param>
        /// <returns>原始 URL</returns>
        string GetRawUrl(HttpRequest request);

        /// <summary>
        /// 获取请求是否通过 AJAX 发出
        /// </summary>
        /// <param name="request">HTTP 请求</param>
        /// <returns>结果</returns>
        bool IsAjaxRequest(HttpRequest request);
    }

    public class WebHelper : IWebHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 检查当前HTTP请求是否可用
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsRequestAvailable()
        {
            if (_httpContextAccessor?.HttpContext == null)
                return false;

            try
            {
                if (_httpContextAccessor.HttpContext.Request == null)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 如果存在，则获取 URL 引荐来源地址
        /// </summary>
        /// <returns>URL 引荐来源地址</returns>
        public virtual string GetUrlReferrer()
        {
            if (!IsRequestAvailable())
                return string.Empty;

            //URL referrer 某些情况下为空（例如在 IE 8 中）
            return _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Referer];
        }

        /// <summary>
        /// 获取一个值，该值指示当前连接是否安全
        /// </summary>
        /// <returns></returns>
        public virtual bool IsCurrentConnectionSecured()
        {
            if (!IsRequestAvailable())
                return false;

            HostingConfig hostingConfig = new();
            //检查主机是否使用负载均衡器
            //使用 HTTP_CLUSTER_HTTPS？
            if (hostingConfig.UseHttpClusterHttps)
                return _httpContextAccessor.HttpContext.Request.Headers[HttpDefaults.HttpClusterHttpsHeader].ToString().Equals("on", StringComparison.OrdinalIgnoreCase);

            //使用 HTTP_X_FORWARDED_PROTO？
            if (hostingConfig.UseHttpXForwardedProto)
                return _httpContextAccessor.HttpContext.Request.Headers[HttpDefaults.HttpXForwardedProtoHeader].ToString().Equals("https", StringComparison.OrdinalIgnoreCase);

            return _httpContextAccessor.HttpContext.Request.IsHttps;
        }

        /// <summary>
        /// 获取存储主机位置
        /// </summary>
        /// <param name="useSsl">是否获取 SSL 安全 URL</param>
        /// <returns>存储主机位置</returns>
        public virtual string GetStoreHost(bool useSsl)
        {
            if (!IsRequestAvailable())
                return string.Empty;

            //尝试从请求的 HOST 报头获取主机
            var hostHeader = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Host];
            if (StringValues.IsNullOrEmpty(hostHeader))
                return string.Empty;

            //为 URL 添加协议方案
            var storeHost = $"{(useSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp)}{Uri.SchemeDelimiter}{hostHeader.FirstOrDefault()}";

            //确保主机以斜杠结尾
            storeHost = $"{storeHost.TrimEnd('/')}/";

            return storeHost;
        }

        /// <summary>
        /// 如果请求的资源属于典型的无需由 CMS 引擎处理的资源，则返回 true。
        /// </summary>
        /// <returns>如果请求目标是静态资源文件，则为 true。</returns>
        public virtual bool IsStaticResource()
        {
            if (!IsRequestAvailable())
                return false;

            string path = _httpContextAccessor.HttpContext.Request.Path;

            //一个小变通方法。FileExtensionContentTypeProvider 包含大多数静态文件扩展名，因此我们可以使用它
            //来源：https://github.com/aspnet/StaticFiles/blob/dev/src/Microsoft.AspNetCore.StaticFiles/FileExtensionContentTypeProvider.cs
            //如果它能返回内容类型，则说明这是一个静态文件
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            return contentTypeProvider.TryGetContentType(path, out var _);
        }

        /// <summary>
        /// 按名称获取查询字符串值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="name">查询参数名称</param>
        /// <returns>查询字符串值</returns>
        public virtual T QueryString<T>(string name)
        {
            if (!IsRequestAvailable())
                return default;

            if (StringValues.IsNullOrEmpty(_httpContextAccessor.HttpContext.Request.Query[name]))
                return default;

            return CommonHelper.To<T>(_httpContextAccessor.HttpContext.Request.Query[name].ToString());
        }

        /// <summary>
        /// 获取一个值，该值指示客户端是否被重定向到新位置
        /// </summary>
        public virtual bool IsRequestBeingRedirected
        {
            get
            {
                var response = _httpContextAccessor.HttpContext.Response;
                //ASP.NET 4 风格 - return response.IsRequestBeingRedirected;
                int[] redirectionStatusCodes = { StatusCodes.Status301MovedPermanently, StatusCodes.Status302Found };

                return redirectionStatusCodes.Contains(response.StatusCode);
            }
        }

        /// <summary>
        /// 获取当前HTTP请求协议
        /// </summary>
        public virtual string GetCurrentRequestProtocol()
        {
            return IsCurrentConnectionSecured() ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        }

        /// <summary>
        /// 获取请求的原始路径和完整查询
        /// </summary>
        /// <param name="request">HTTP 请求</param>
        /// <returns>原始 URL</returns>
        public virtual string GetRawUrl(HttpRequest request)
        {
            //首先尝试从请求特性获取原始目标
            //注意：该值尚未经过 UrlDecode 解码
            var rawUrl = request.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;

            //或者手动组合原始 URL
            if (string.IsNullOrEmpty(rawUrl))
                rawUrl = $"{request.PathBase}{request.Path}{request.QueryString}";

            return rawUrl;
        }

        /// <summary>
        /// 判断是否为AJAX请求
        /// </summary>
        /// <param name="request">HTTP 请求</param>
        /// <returns>结果</returns>
        public virtual bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers == null)
                return false;

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}