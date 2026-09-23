using My.XXX.Services.Authentication.Models;
using FluentResults;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Common;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Ports;
using My.XXX.Services.Menus;
using My.XXX.Services.Menus.Models;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class MenuUseCaseTests
{
    [TestMethod]
    public async Task CombinedRoleAccessCommitsOnceAndRollsBackSecondStepFailures()
    {
        var store = Store();
        var role = Guid.NewGuid();
        var useCase = new RoleAccessAdministration(store, new RoleMenuMutations(TimeProvider.System),
            new PermissionMutations(), new CurrentUser());
        Assert.IsTrue((await useCase.ReplaceAsync(role, new() { 1 }, new() { "menu.add", "menu.add" })).IsSuccess);
        Assert.AreEqual(1, store.Transactions);
        Assert.AreEqual(1L, store.Revision);
        CollectionAssert.AreEqual(new[] { "menu.add" }, store.Codes);
        var invalid = await useCase.ReplaceAsync(role, new() { 2 }, new() { "unknown" });
        Assert.AreEqual("Permission.InvalidInput", Code(invalid));
        CollectionAssert.AreEqual(new[] { 1 }, store.Grants);
        CollectionAssert.AreEqual(new[] { "menu.add" }, store.Codes);
        Assert.AreEqual(1L, store.Revision);
        store.ThrowOnPermissionWrite = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ReplaceAsync(role, new() { 2 }, new() { "menu.edit" }));
        CollectionAssert.AreEqual(new[] { 1 }, store.Grants);
        CollectionAssert.AreEqual(new[] { "menu.add" }, store.Codes);
        Assert.AreEqual(1L, store.Revision);
        store.ThrowOnPermissionWrite = false;
        Assert.IsTrue((await useCase.ReplaceAsync(role, new(), new())).IsSuccess);
        Assert.HasCount(0, store.Grants);
        Assert.HasCount(0, store.Codes);
        Assert.AreEqual(2L, store.Revision);
    }

    private sealed class CurrentUser : My.XXX.Services.Abstractions.Interfaces.ICurrentUser
    {
        public UserIdentity User => new() { UserId = "editor" };
    }

    [TestMethod]
    public async Task InvalidParentAndMissingMenuKeepDistinctErrorsAndNeverWrite()
    {
        var store = Store();
        var useCase = new MenuCommands(store, TimeProvider.System);
        var missing = (await useCase.Update(new EditMenu { Id = 99 }, "en-US", "editor"));
        var cycle = (await useCase.Update(new EditMenu { Id = 1, ParentId = 2 }, "en-US", "editor"));
        Assert.AreEqual("Menu.NotFound", Code(missing));
        Assert.AreEqual("Menu.InvalidParent", Code(cycle));
        Assert.AreEqual("Invalid menu parent.", cycle.ToApiResult().Message);
        Assert.AreEqual(0, store.Writes);
        Assert.AreEqual(0L, store.Revision);
        Assert.AreEqual(2, store.Transactions, "State-dependent validation must execute inside the transaction.");
    }

    [TestMethod]
    public async Task PartialUpdateClearsExplicitFieldsAndPreservesSnapshotAndOtherTranslations()
    {
        var store = Store();
        var before = store.Menus[0];
        var instant = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);
        var useCase = new MenuCommands(store, new FixedClock(instant));
        var result = (await useCase.Update(new EditMenu { Id = 1, DisplayName = " 中文 ", Description = "ignored", ClearFields = new() { "description" } }, "zh-CN", "editor"));
        Assert.IsTrue(result.IsSuccess);
        var updated = store.Menus[0];
        Assert.IsNull(updated.Description);
        Assert.AreEqual("icon", updated.Icon);
        Assert.AreEqual("description", before.Description, "Use cases must not mutate loaded snapshots.");
        Assert.AreEqual("English", updated.DisplayNames.Values["en-US"]);
        Assert.AreEqual("中文", updated.DisplayNames.Values["zh-CN"]);
        Assert.AreEqual(instant.DateTime, updated.UpdatedTime);
        Assert.AreEqual("editor", updated.UpdatedBy);
        Assert.AreEqual(1L, store.Revision);
    }

    [TestMethod]
    public async Task RoleReplacementRejectsUnknownMenusAndEmptyReplacementRemovesAll()
    {
        var store = Store();
        var role = Guid.NewGuid();
        store.Grants.Add(1);
        var useCase = new RoleMenuAssignmentService(store, new RoleMenuMutations(TimeProvider.System), new CurrentUser(), TimeProvider.System);
        Assert.AreEqual("Menu.InvalidSelection", Code((await useCase.SetRoleMenus(role, new() { 99 }, RoleMenuChange.Replace))));
        CollectionAssert.AreEqual(new[] { 1 }, store.Grants);
        Assert.IsTrue((await useCase.SetRoleMenus(role, new() { 2, 2, 3 }, RoleMenuChange.Replace)).IsSuccess);
        CollectionAssert.AreEquivalent(new[] { 2, 3 }, store.Grants);
        Assert.IsTrue((await useCase.SetRoleMenus(role, new(), RoleMenuChange.Replace)).IsSuccess);
        Assert.HasCount(0, store.Grants);
        Assert.AreEqual(2L, store.Revision);
    }

    [TestMethod]
    public async Task RejectedOrThrowingMultiRowWritesRollBackEarlierChangesAndRevision()
    {
        var store = Store();
        store.Menus[1].ParentId = 0;
        store.FailOnWrite = 2;
        var useCase = new MenuCommands(store, TimeProvider.System);
        var result = (await useCase.Move(new MenuSortModel { CurrentId = 1, PrevId = 3 }, "editor"));
        Assert.AreEqual("Menu.WriteFailed", Code(result));
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, store.Menus.Select(m => m.Number).ToArray());
        Assert.AreEqual(0L, store.Revision);
        store.Writes = 0;
        store.ThrowOnFailure = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await useCase.Move(new MenuSortModel { CurrentId = 1, PrevId = 3 }, "editor"); });
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, store.Menus.Select(m => m.Number).ToArray());
        Assert.AreEqual(0L, store.Revision);
    }

    [TestMethod]
    public async Task InvalidInputIsRejectedBeforeOpeningTheTransactionAndCancellationIsForwarded()
    {
        var store = Store();
        var context = new CommandContext("en-US", "editor");
        var commands = new MenuCommandService(store, new MenuMutations(TimeProvider.System), context, context);
        Assert.IsTrue((await commands.Add(null)).IsFailed);
        Assert.IsTrue((await commands.Update(new EditMenu { Id = 0 })).IsFailed);
        Assert.IsTrue((await commands.Remove(new())).IsFailed);
        Assert.AreEqual(0, store.Transactions);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => commands.Update(new EditMenu { Id = 1 }, cancellation.Token));
        Assert.AreEqual(0L, store.Revision);
        Assert.AreEqual(0, store.Writes);
    }

    private sealed class CommandContext(string culture, string user) : My.XXX.Services.Abstractions.Interfaces.ICurrentUser, My.XXX.Services.Abstractions.Interfaces.ICurrentCulture
    {
        public UserIdentity User => new() { UserId = user };
        public string CultureName => culture;
    }
    private sealed class MenuCommands(IAccessControlTransaction transaction, TimeProvider clock)
    {
        private MenuCommandService Create(string culture, string user)
        {
            var context = new CommandContext(culture, user);
            return new(transaction, new MenuMutations(clock), context, context);
        }
        public Task<Result> Update(EditMenu input, string culture, string user) => Create(culture, user).Update(input);
        public Task<Result> Move(MenuSortModel input, string user) => Create("en-US", user).UpdateSort(input);
    }

    private static string Code(Result result) => result.Errors.OfType<BusinessError>().Single().Code;
    private static MemoryTransaction Store() => new()
    {
        Menus = new()
        {
            new() { Id = 1, Number = 0, DisplayName = "English", DisplayNames = new LocalizedText(new Dictionary<string, string> { ["en-US"] = "English" }), Description = "description", Icon = "icon" },
            new() { Id = 2, Number = 1, ParentId = 1 }, new() { Id = 3, Number = 2 }
        }
    };
    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class MemoryTransaction : IAccessControlTransaction, IAccessControlWriteSession
    {
        public List<MenuState> Menus = new();
        public List<int> Grants = new();
        public List<string> Codes = new();
        public bool ThrowOnPermissionWrite;
        public long Revision;
        public int Writes, Transactions, FailOnWrite;
        public bool ThrowOnFailure;
        private bool active;
        public async Task<Result> Execute(Func<IAccessControlWriteSession, Task<Result>> action, CancellationToken cancellationToken = default)
        {
            Assert.IsFalse(active);
            Transactions++;
            active = true;
            var menus = Menus.Select(m => m.Copy()).ToList();
            var grants = Grants.ToList();
            var codes = Codes.ToList();
            var committed = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await action(this);
                cancellationToken.ThrowIfCancellationRequested();
                committed = result.IsSuccess;
                if (committed) Revision++;
                return result;
            }
            finally
            {
                if (!committed) { Menus = menus; Grants = grants; Codes = codes; }
                active = false;
            }
        }
        public async Task<List<MenuState>> LoadMenus(CancellationToken cancellationToken = default) { Assert.IsTrue(active); return Menus.Select(m => m.Copy()).ToList(); }
        public Task ReplaceRolePermissions(Guid roleId, List<string> codes, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(active);
            Codes.Clear();
            if (ThrowOnPermissionWrite) throw new InvalidOperationException("injected permission failure");
            Codes.AddRange(codes);
            return Task.CompletedTask;
        }
        public Task<List<int>> LoadMenuIds(CancellationToken cancellationToken = default) { Assert.IsTrue(active); return Task.FromResult(Menus.Select(m => m.Id).ToList()); }
        public async Task<List<int>> LoadRoleMenus(Guid roleId, CancellationToken cancellationToken = default) { Assert.IsTrue(active); return Grants.ToList(); }
        public async Task<int> Insert(MenuState menu, CancellationToken cancellationToken = default) { Assert.IsTrue(active); Writes++; Menus.Add(menu.Copy()); return 1; }
        public async Task<int> Update(MenuState menu, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(active);
            if (++Writes == FailOnWrite)
            {
                if (ThrowOnFailure) throw new InvalidOperationException("injected");
                return 0;
            }
            Menus[Menus.FindIndex(m => m.Id == menu.Id)] = menu.Copy();
            return 1;
        }
        public async Task<int> Remove(List<int> ids, string userId, DateTime timestamp, CancellationToken cancellationToken = default) { Assert.IsTrue(active); Writes++; return Menus.RemoveAll(m => ids.Contains(m.Id)); }
        public async Task<bool> ApplyRoleChanges(Guid roleId, List<int> additions, List<int> removals, string userId, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(active);
            Writes++;
            Grants.RemoveAll(removals.Contains);
            Grants.AddRange(additions);
            return true;
        }
    }
}
