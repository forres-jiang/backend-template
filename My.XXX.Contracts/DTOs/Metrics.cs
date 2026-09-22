using System;
using System.Diagnostics;

namespace My.XXX.Contracts.DTOs
{
    public class MetricsInfo
    {
        public string AppCode { get; set; }
        public string HostName { get; set; }
        public DateTime CreateTime { get; set; }
        public Guid? RequestId { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public double TotalTime { get; set; }
        public string ClientIP { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        // 仅允许已序列化的数据；框架对象不能跨越此边界。
        public string Inputs { get; set; } = "null";
        public string Url { get; set; }
        public string ReturnValue { get; set; } = "null";
        public string RequestType { get; set; }
        public bool IsException { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public class MetricsObject
    {
        public MetricsInfo LogRecord { get; set; }
        public Stopwatch Watch { get; set; }
    }
}
