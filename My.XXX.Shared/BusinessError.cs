using FluentResults;

namespace My.XXX.Shared;

public enum BusinessErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

/// <summary>业务失败错误，带有可选的公开响应码（并非 HTTP 状态码）。</summary>
public sealed class BusinessError : Error
{
    public int StatusCode { get; }
    public string Code { get; }
    public BusinessErrorKind Kind { get; }

    public BusinessError(string message, int statusCode = 0, string code = null,
        BusinessErrorKind kind = BusinessErrorKind.Validation) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Kind = kind;
    }
}
