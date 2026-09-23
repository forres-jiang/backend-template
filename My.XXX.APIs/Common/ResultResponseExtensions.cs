using FluentResults;
using My.XXX.APIs.Models;
using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.APIs.Common;

/// <summary>将业务结果映射到现有的公开响应契约。</summary>
public static class ResultResponseExtensions
{
    /// <summary>新端点同时使用 HTTP 状态码和显式响应信封；旧端点保留其原有状态码契约。</summary>
    public static Microsoft.AspNetCore.Mvc.ActionResult<ApiResponse<bool>> ToHttpResult(this Result result, string traceId = null)
    {
        var response = new ApiResponse<bool>(result.IsSuccess ? 1 : 0,
            result.IsSuccess ? "Success" : string.Join(",", result.Errors.Select(e => e.Message)), result.IsSuccess,
            ErrorCode(result), traceId);
        if (result.IsSuccess) return new Microsoft.AspNetCore.Mvc.OkObjectResult(response);
        return new Microsoft.AspNetCore.Mvc.ObjectResult(response) { StatusCode = HttpStatus(result) };
    }
    public static Microsoft.AspNetCore.Mvc.ActionResult<ApiResponse<T>> ToHttpResult<T>(this Result<T> result, string traceId = null)
    {
        var response = new ApiResponse<T>(result.IsSuccess ? 1 : 0,
            result.IsSuccess ? "Success" : string.Join(",", result.Errors.Select(e => e.Message)),
            result.IsSuccess ? result.Value : default, ErrorCode(result), traceId);
        return new Microsoft.AspNetCore.Mvc.ObjectResult(response) { StatusCode = result.IsSuccess ? 200 : HttpStatus(result) };
    }
    private static string ErrorCode(ResultBase result) => result.IsSuccess ? null
        : result.Errors.OfType<BusinessError>().FirstOrDefault()?.Code ?? "Request.Failed";
    private static int HttpStatus(ResultBase result) => result.Errors.OfType<BusinessError>().FirstOrDefault()?.Code switch
    {
        "Menu.NotFound" => 404,
        "Menu.HasChildren" => 409,
        "Menu.WriteFailed" => 409,
        "Authentication.InvalidToken" or "Authentication.RefreshRejected" => 401,
        _ => 400
    };
    public static MyResult<bool> ToBooleanApiResult(this Result result) => result.IsSuccess
        ? MyResult<bool>.Success(true)
        : new MyResult<bool>(0, string.Join(",", result.Errors.Select(error => error.Message)), false);
    public static MyResult ToApiResult(this Result result) =>
        result.IsSuccess ? MyResult.Success() : Failure(result);

    public static MyResult ToApiResult<T>(this Result<T> result) =>
        result.IsSuccess ? MyResult.Success(result.Value) : Failure(result);

    public static LoginResult ToLoginResult(this Result<AuthenticationSession> result)
    {
        if (result.IsFailed) return LoginResult.Fail(string.Join(",", result.Errors.Select(error => error.Message)));
        var session = result.Value;
        return LoginResult.Success(session.User, session.Tokens.AccessToken,
            session.Tokens.RefreshToken, session.Tokens.ExpiryInMinutes);
    }

    private static MyResult Failure(ResultBase result)
    {
        // 保留第一个显式的业务错误码；此处不设置 HTTP 状态码。
        var code = result.Errors.OfType<BusinessError>().FirstOrDefault()?.StatusCode ?? 0;
        return MyResult.Fail(string.Join(",", result.Errors.Select(error => error.Message)), code);
    }
}
