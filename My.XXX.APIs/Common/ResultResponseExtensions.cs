using FluentResults;
using My.XXX.Infra;
using System.Linq;

namespace My.XXX.APIs.Common;

/// <summary>Maps business results to the existing public response contract.</summary>
public static class ResultResponseExtensions
{
    public static MyResult ToApiResult(this Result result) =>
        result.IsSuccess ? MyResult.Success() : Failure(result);

    public static MyResult ToApiResult<T>(this Result<T> result) =>
        result.IsSuccess ? MyResult.Success(result.Value) : Failure(result);

    private static MyResult Failure(ResultBase result)
    {
        // Preserve the first explicit business code; this does not set HTTP status.
        var code = result.Errors.OfType<BusinessError>().FirstOrDefault()?.StatusCode ?? 0;
        return MyResult.Fail(string.Join(",", result.Errors.Select(error => error.Message)), code);
    }
}
