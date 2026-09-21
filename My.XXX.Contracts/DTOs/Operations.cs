using System;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Contracts.DTOs
{
    public class OperationDto
    {
        public string AppCode { get; set; }
        public string HostName { get; set; }
        public DateTime CreateTime { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public double TotalTime { get; set; }
        public string ClientIP { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Inputs { get; set; }
        public string Url { get; set; }
        public string ReturnValue { get; set; }
        public Guid? RequestId { get; set; }
        public string RequestType { get; set; }
        public bool IsException { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public class OperationQeury : QueryBase
    {
        [DataType(DataType.Text)]
        public string Controller { get; set; }

        [DataType(DataType.Text)]
        public string Action { get; set; }

        public DateTime? Date { get; set; }
    }
}
