namespace My.XXX.APIs.Configurations
{
    public class HostingConfig
    {
        /// <summary>
        /// 获取或设置一个值，该值指示是否使用 HTTP_CLUSTER_HTTPS
        /// </summary>
        public bool UseHttpClusterHttps { get; set; } = false;

        /// <summary>
        /// 获取或设置一个值，该值指示是否使用 HTTP_X_FORWARDED_PROTO
        /// </summary>
        public bool UseHttpXForwardedProto { get; set; } = false;

        /// <summary>
        /// 获取或设置自定义转发的HTTP报头（例如 CF-Connecting-IP、X-FORWARDED-PROTO 等）
        /// </summary>
        public string ForwardedHttpHeader { get; set; } = string.Empty;
    }

    public static class HttpDefaults
    {
        public static string DefaultHttpClient => "default";
        public static string HttpClusterHttpsHeader => "HTTP_CLUSTER_HTTPS";
        public static string HttpXForwardedProtoHeader => "X-Forwarded-Proto";
        public static string XForwardedForHeader => "X-FORWARDED-FOR";
    }
}