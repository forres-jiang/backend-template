using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Services;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Mapping;
using My.XXX.Services.Models;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.UnitTests;

[TestClass]
public class MenuQueryBoundaryTests
{
    [TestMethod]
    public async Task PickerNormalizesCriteriaWithoutMutatingInputOrRepositoryState()
    {
        var repository = new ReadRepository();
        var service = new MenuQueryService(repository, new Culture(), new ApplicationMapper());
        var input = new QueryMenu { PageIndex = 2, PageSize = 20, IsAction = true, IsDisplay = false };
        var result = await service.SearchMenus(input);
        Assert.IsFalse(repository.Criteria.IsAction.Value);
        Assert.IsTrue(repository.Criteria.IsDisplay);
        Assert.AreEqual(1, repository.Criteria.PageIndex);
        Assert.IsTrue(input.IsAction.Value);
        Assert.IsFalse(input.IsDisplay);
        Assert.AreEqual(1, input.PageIndex);
        Assert.AreEqual("English", repository.State.DisplayName);
        Assert.AreEqual("中文", result.List[0].DisplayName);
        Assert.AreEqual(7, result.List[0].Value);
    }

    private sealed class Culture : ICurrentCulture { public string CultureName => "zh-CN"; }
    private sealed class ReadRepository : IMenuReadRepository
    {
        public MenuSearch Criteria;
        public MenuState State = new() { Id = 7, DisplayName = "English", DisplayNames = "{\"zh-CN\":\"中文\"}" };
        public async Task<Paged<MenuState>> Search(MenuSearch criteria, CancellationToken cancellationToken = default) { Criteria = criteria; return Paged<MenuState>.Create(new() { State }, 1); }
        public async Task<MenuState> Get(int id, CancellationToken cancellationToken = default) => State;
        public async Task<List<MenuState>> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null, CancellationToken cancellationToken = default) => new() { State };
        public async Task<List<MenuState>> GetRoleMenuByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default) => new() { State };
    }
}
