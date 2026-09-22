using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Menus.Models;
using My.XXX.Services.Operations;
using My.XXX.Services.Operations.Models;
using My.XXX.Services.Operations.Ports;
using My.XXX.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class QueryContractTests
{
    [TestMethod]
    [DataRow(2, 20, 20, 20)]
    [DataRow(0, 200, 0, 100)]
    [DataRow(-1, 0, 0, 1)]
    public async Task OperationPagingIsNormalizedWithoutChangingRawInput(int page, int size, int offset, int limit)
    {
        var repository = new Repository();
        var input = new OperationQeury { PageIndex = page, PageSize = size, Controller = "Menu" };
        await new OperationService(repository).GetRequestLogs(input);
        Assert.AreEqual(page, input.PageIndex);
        Assert.AreEqual(size, input.PageSize);
        Assert.AreEqual(offset, repository.Query.Offset);
        Assert.AreEqual(limit, repository.Query.Limit);
        Assert.AreEqual("Menu", repository.Query.Controller);
    }

    [TestMethod]
    public void LocalizedValuesDoNotMutateOriginalOrCopiedMenuState()
    {
        var source = new Dictionary<string, string> { ["en-US"] = "English" };
        var menu = new MenuState { DisplayNames = new LocalizedText(source) };
        source["en-US"] = "changed";
        var copy = menu.Copy();
        copy.DisplayNames = copy.DisplayNames.With("en-us", "Updated");
        Assert.AreEqual("English", menu.DisplayNames.Values["en-US"]);
        Assert.AreEqual("Updated", copy.DisplayNames.Values["EN-US"]);
    }
    private sealed class Repository : IOperationRepository
    {
        public OperationSearch Query;
        public Task Save(MetricsInfo operation) => Task.CompletedTask;
        public Task<Paged<OperationDto>> Search(OperationSearch query)
        { Query = query; return Task.FromResult(Paged<OperationDto>.Create(new(), 0)); }
    }
}
