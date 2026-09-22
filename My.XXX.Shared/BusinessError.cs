using FluentResults;

namespace My.XXX.Shared;

/// <summary>业务失败错误，带有可选的公开响应码（并非 HTTP 状态码）。</summary>
public sealed class BusinessError : Error
{
    public int StatusCode { get; }
    public string Code { get; }

    public BusinessError(string message, int statusCode = 0, string code = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
