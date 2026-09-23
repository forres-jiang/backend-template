using System.Diagnostics;

namespace My.XXX.APIs.Models;

/// <summary>Explicit response contract for versioned endpoints. Legacy envelopes are unchanged.</summary>
public sealed class ApiResponse<T> : MyResult<T>
{
    public string Code { get; }
    public string TraceId { get; }
    public ApiResponse(int statusCode, string message, T data, string code = null, string traceId = null)
        : base(statusCode, message, data)
    {
        Code = code;
        TraceId = traceId ?? Activity.Current?.TraceId.ToString();
    }
}
