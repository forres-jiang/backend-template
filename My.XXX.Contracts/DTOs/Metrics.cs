using System;
using System.Diagnostics;

namespace My.XXX.Service.DTOs
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
        // Already serialized data only; framework objects cannot cross this boundary.
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
