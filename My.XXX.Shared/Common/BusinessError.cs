using FluentResults;

namespace My.XXX.Shared;

/// <summary>A business failure with an optional public response code (not an HTTP status).</summary>
public sealed class BusinessError : Error
{
    public int StatusCode { get; }

    public BusinessError(string message, int statusCode = 0) : base(message)
    {
        StatusCode = statusCode;
    }
}
