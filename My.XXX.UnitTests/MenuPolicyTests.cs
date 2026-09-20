using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Models;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.UnitTests;

[TestClass]
public class MenuPolicyTests
{
    [TestMethod]
    public void RejectsCyclesMissingParentsAndChildrenUnderActions()
    {
        var menus = new List<MenuState>
        {
            new() { Id = 1 }, new() { Id = 2, ParentId = 1 }, new() { Id = 3, ParentId = 2 },
            new() { Id = 4, ParentId = 1, IsAction = true }
        };
        Assert.IsFalse(MenuHierarchy.CanPlace(menus, 1, 3, false));
        Assert.IsFalse(MenuHierarchy.CanPlace(menus, 5, 99, false));
        Assert.IsFalse(MenuHierarchy.CanPlace(menus, 5, 4, true));
        Assert.IsFalse(MenuHierarchy.CanPlace(menus, 2, 1, true));
        Assert.IsTrue(MenuHierarchy.CanPlace(menus, 5, 2, true));
        Assert.IsNull(MenuOrder.Build(menus, new MenuSortModel { CurrentId = 1, PrevId = 3 }));
    }
    [TestMethod]
    public void TreeRetainsOrderAndDoesNotLoopOnCorruptCycles()
    {
        var menus = new List<MenuDto>
        {
            new() { Id = 1, Number = 2 }, new() { Id = 2, Number = 1 }, new() { Id = 3, ParentId = 1 },
            new() { Id = 4, ParentId = 5 }, new() { Id = 5, ParentId = 4 }
        };
        var tree = MenuTree.Build(menus);
        CollectionAssert.AreEqual(new[] { 2, 1 }, tree.Select(m => m.Id).ToArray());
        Assert.AreEqual(3, tree[1].Children.Single().Id);
    }
    [TestMethod]
    public void LocalizationFallsBackForLegacyInvalidOrMissingValues()
    {
        Assert.AreEqual("fallback", MenuDisplayNames.Get("not-json", "zh-CN", "fallback"));
        Assert.AreEqual("fallback", MenuDisplayNames.Get(null, "zh-CN", "fallback"));
        var value = MenuDisplayNames.Set("{\"en-US\":\"English\"}", "zh-CN", " 中文 ");
        Assert.AreEqual("English", MenuDisplayNames.Get(value, "en-us", "fallback"));
        Assert.AreEqual("中文", MenuDisplayNames.Get(value, "ZH-cn", "fallback"));
        Assert.IsFalse(MenuUpdateFields.Valid(new[] { "CreatedBy" }));
        Assert.IsTrue(MenuUpdateFields.Valid(new[] { "Description", "url" }));
    }
}
