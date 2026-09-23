using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.Menus;
using My.XXX.Services.Menus.Mapping;
using My.XXX.Services.Menus.Models;
using My.XXX.Services.Menus.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class MenuQueryBoundaryTests
{
    [TestMethod]
    public async Task MissingMenuIsAnExplicitFailureForNewCallersAndNullForLegacyCallers()
    {
        var service = new MenuQueryService(new ReadRepository { State = null }, new Culture(), new ApplicationMapper());
        var result = await service.FindAsync(999);
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual("Menu.NotFound", ((BusinessError)result.Errors[0]).Code);
        var http = (Microsoft.AspNetCore.Mvc.ObjectResult)My.XXX.APIs.Common.ResultResponseExtensions.ToHttpResult(result).Result;
        Assert.AreEqual(404, http.StatusCode);
        var legacy = new My.XXX.Services.Compatibility.MenuService(null, service, null);
        Assert.IsNull(await legacy.Get(999));
    }
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
        Assert.AreEqual(2, input.PageIndex);
        Assert.AreEqual("English", repository.State.DisplayName);
        Assert.AreEqual("中文", result.List[0].DisplayName);
        Assert.AreEqual(7, result.List[0].Value);
    }

    [TestMethod]
    public async Task TypedTreeProjectionPreservesLegacyJsonActionsChecksAndSnapshots()
    {
        MenuState Menu(int id, int parent, string name, bool action = false) => new()
        {
            Id = id, ParentId = parent, DisplayName = "fallback", IsDisplay = true, IsAction = action,
            DisplayNames = new LocalizedText(new Dictionary<string, string> { ["zh-CN"] = name, ["en-US"] = "English" })
        };
        var repository = new ReadRepository
        {
            States = new() { Menu(1, 0, "根"), Menu(2, 1, "子"), Menu(3, 2, "操作", true) }
        };
        var service = new MenuQueryService(repository, new Culture(), new ApplicationMapper());
        var roles = await service.GetMenuByRoles(new RoleMenuQuery { RoleIds = new() { Guid.NewGuid() } });
        var child = roles.Single().Children.Single();
        Assert.AreEqual("根", roles[0].DisplayName);
        Assert.AreEqual("子", child.DisplayName);
        Assert.AreEqual("操作", child.Actions.Single().DisplayName);
        Assert.HasCount(0, child.Children);
        Assert.IsNull(child.Actions[0].Children);
        Assert.AreEqual("English", My.XXX.Contracts.Serialization.LocalizedNamesJson.Read(child.DisplayNames)["en-US"]);
        var checkedTree = await service.GetMenuTreeCheckedByRoles(new());
        Assert.IsTrue(checkedTree[0].Checked);
        Assert.IsTrue(checkedTree[0].Children[0].Children[0].Checked);
        Assert.IsTrue(repository.States.All(menu => menu.DisplayName == "fallback"));
        var tree = await service.GetTreeMenus(true);
        Assert.AreEqual("操作", tree[0].Children[0].Children[0].DisplayName);
    }

    private sealed class Culture : ICurrentCulture { public string CultureName => "zh-CN"; }
    private sealed class ReadRepository : IMenuReadRepository
    {
        public MenuSearch Criteria;
        public List<MenuState> States;
        public MenuState State = new() { Id = 7, DisplayName = "English", DisplayNames = new LocalizedText(new Dictionary<string, string> { ["zh-CN"] = "中文" }) };
        public async Task<Paged<MenuState>> Search(MenuSearch criteria, CancellationToken cancellationToken = default) { Criteria = criteria; return Paged<MenuState>.Create(new() { State }, 1); }
        public async Task<MenuState> Get(int id, CancellationToken cancellationToken = default) => State;
        public async Task<List<MenuState>> GetMenus(int? parentId = null, List<int> ids = null, bool? isDisplay = null, CancellationToken cancellationToken = default) => States ?? new() { State };
        public async Task<List<MenuState>> GetRoleMenuByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default) => States ?? new() { State };
    }
}
