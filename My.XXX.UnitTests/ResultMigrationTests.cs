using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs;
using My.XXX.APIs.Common;
using My.XXX.Infra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.Tests;

[TestClass]
public class ResultMigrationTests
{
    [TestMethod]
    public void ConversionPreservesSuccessAndFailureJson()
    {
        AssertJson(MyResult.Success(), Result.Ok().ToApiResult());
        AssertJson(MyResult.Success(new { Id = 7 }), Result.Ok(new { Id = 7 }).ToApiResult());
        AssertJson(MyResult.Success(null), Result.Ok<string>(null).ToApiResult());
        AssertJson(MyResult.Fail("invalid"), Result.Fail<int>("invalid").ToApiResult());
        AssertJson(MyResult.Fail("one,two"), Result.Fail(new[] { "one", "two" }).ToApiResult());
        AssertJson(MyResult.Fail("conflict", 409), Result.Fail(new BusinessError("conflict", 409)).ToApiResult());
    }

    [TestMethod]
    public void ConversionDoesNotExposeInternalErrorDetails()
    {
        var error = new Error("Save failed.").WithMetadata("Internal", "secret")
            .CausedBy(new Error("database details"));
        AssertJson(MyResult.Fail("Save failed."), Result.Fail<string>(error).ToApiResult());
    }

    [TestMethod]
    public async Task FilterLocalizesConvertedFailureWithoutWrappingOrChangingHttpStatus()
    {
        var response = new ObjectResult(Result.Fail(new BusinessError("conflict", 409)).ToApiResult())
        {
            StatusCode = 422
        };
        await ApplyFilter(response);
        var result = (MyResult)response.Value;
        Assert.AreEqual(409, result.StatusCode);
        Assert.AreEqual("localized:conflict", result.Message);
        Assert.IsNull(result.Data);
        Assert.AreEqual(422, response.StatusCode);
    }

    [TestMethod]
    public async Task FilterPreservesLoginFieldsAndBaseResult()
    {
        var login = LoginResult.RefreshSuccess("access", "refresh", 30);
        var response = new ObjectResult(login) { DeclaredType = typeof(MyResult) };
        await ApplyFilter(response);
        Assert.AreSame(login, response.Value);
        Assert.AreEqual("access", login.AccessToken);
        Assert.AreEqual("refresh", login.RefreshToken);
        Assert.AreEqual(30, login.ExpiryInMinutes);
        Assert.AreEqual("localized:Success", login.Message);

        var failure = BaseResult.Fail("invalid");
        var badRequest = new BadRequestObjectResult(failure) { DeclaredType = typeof(BaseResult) };
        await ApplyFilter(badRequest);
        Assert.AreSame(failure, badRequest.Value);
        Assert.AreEqual(400, badRequest.StatusCode);
        Assert.AreEqual("localized:invalid", failure.Message);
    }

    [TestMethod]
    public async Task FilterPreservesHttpStatusWhenWrappingOrdinaryData()
    {
        var response = new ObjectResult("created") { DeclaredType = typeof(string), StatusCode = 201 };
        await ApplyFilter(response);
        Assert.AreEqual(201, response.StatusCode);
        Assert.AreEqual("created", ((MyResult)response.Value).Data);
    }

    private static void AssertJson(object expected, object actual) =>
        Assert.IsTrue(JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(expected)),
            JToken.Parse(JsonConvert.SerializeObject(actual))));

    private static async Task ApplyFilter(ObjectResult response)
    {
        var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var context = new ResultExecutingContext(action, filters, response, controller);
        var called = false;
        await new UnifyResultAsync(new TestLocalizer()).OnResultExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ResultExecutedContext(action, filters, context.Result, controller));
        });
        Assert.IsTrue(called);
        Assert.AreSame(response, context.Result);
    }

    private sealed class TestLocalizer : IStringLocalizer<ResultResource>
    {
        public LocalizedString this[string name] => new(name, "localized:" + name);
        public LocalizedString this[string name, params object[] arguments] => this[name];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();
    }
}
