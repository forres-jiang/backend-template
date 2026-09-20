using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Service;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Service.Models;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;

namespace My.XXX.UnitTests;

[TestClass]
public class MenuQueryBoundaryTests
{
    [TestMethod]
    public void PickerNormalizesCriteriaWithoutMutatingInputOrRepositoryState()
    {
        var repository = new ReadRepository();
        var service = new MenuQueryService(repository, new Culture(), new ApplicationMapper());
        var input = new QueryMenu { PageIndex = 2, PageSize = 20, IsAction = true, IsDisplay = false };
        var result = service.SearchMenus(input);
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
        public Paged<MenuState> Search(MenuSearch criteria) { Criteria = criteria; return Paged<MenuState>.Create(new() { State }, 1); }
        public MenuState Get(int id) => State;
        public List<MenuState> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null) => new() { State };
        public List<MenuState> GetRoleMenuByRoles(List<Guid> roleIds) => new() { State };
    }
}
