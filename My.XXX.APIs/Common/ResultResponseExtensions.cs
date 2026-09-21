using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.APIs.Common;

/// <summary>Maps business results to the existing public response contract.</summary>
public static class ResultResponseExtensions
{
    /// <summary>New endpoints use HTTP status as well as the explicit envelope; legacy endpoints retain their status contract.</summary>
    public static Microsoft.AspNetCore.Mvc.ActionResult<MyResult<bool>> ToHttpResult(this Result result)
    {
        var response = result.ToBooleanApiResult();
        if (result.IsSuccess) return new Microsoft.AspNetCore.Mvc.OkObjectResult(response);
        var code = result.Errors.OfType<BusinessError>().FirstOrDefault()?.Code;
        var status = code switch
        {
            "Menu.NotFound" => 404,
            "Menu.HasChildren" => 409,
            "Menu.WriteFailed" => 409,
            _ => 400
        };
        return new Microsoft.AspNetCore.Mvc.ObjectResult(response) { StatusCode = status };
    }
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
        // Preserve the first explicit business code; this does not set HTTP status.
        var code = result.Errors.OfType<BusinessError>().FirstOrDefault()?.StatusCode ?? 0;
        return MyResult.Fail(string.Join(",", result.Errors.Select(error => error.Message)), code);
    }
}
