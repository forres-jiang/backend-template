using FluentResults;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.APIs.Common;

/// <summary>Maps business results to the existing public response contract.</summary>
public static class ResultResponseExtensions
{
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
