using LinqToDB;
using LinqToDB.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Persistences;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Persistences.Repositories;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Ports;
using My.XXX.Services.Menus;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace My.XXX.IntegrationTests;

/// <summary>需要显式提供一次性的数据库服务器；每个测试用例都会创建自己的数据库。</summary>
[TestClass]
public class MenuConcurrencyTests
{
    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task FullMigrationSupportsAuthenticationAndStablePermissions(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var authentication = new AuthenticationStore(db);
        var role = Guid.NewGuid();
        await authentication.SetUserAsync(new UserInfo { UserId = "migration-user", RoleIds = new() { role } }, true);
        await authentication.CreateSessionAsync(new My.XXX.Services.Authentication.Models.SessionState
        {
            SessionId = "migration-session", UserId = "migration-user", RefreshTokenId = "first",
            ExpiresUtc = DateTime.UtcNow.AddHours(1)
        });
        var session = await authentication.GetActiveSessionAsync("migration-session", DateTime.UtcNow);
        CollectionAssert.AreEqual(new[] { role }, session.User.RoleIds);
        var rotations = await Task.WhenAll(Enumerable.Range(0, 2).Select(async i =>
        {
            using var other = fixture.Open();
            return await new AuthenticationStore(other).RotateAsync("migration-session", "first", "next" + i, DateTime.UtcNow);
        }));
        Assert.AreEqual(1, rotations.Count(success => success));
        var permissions = new PermissionStore(db);
        var before = await permissions.GetPermissionRevisionAsync();
        Assert.IsTrue((await new PermissionAdministration(new RolePermissionStore(db), new MenuRepository(db),
            new PermissionMutations()).ReplaceAsync(role, new() { "menu.add" })).IsSuccess);
        Assert.AreEqual(before + 1, await permissions.GetPermissionRevisionAsync());
        CollectionAssert.AreEqual(new[] { "menu.add" }, await permissions.GetPermissionCodesAsync(new() { role }));
        await My.XXX.Persistences.Migrations.MigrationRunner.ApplyAsync(fixture.ConnectionString, DatabaseConfiguration.ParseProvider(providerName));
        Assert.AreEqual(before + 1, await permissions.GetPermissionRevisionAsync(), "Replaying migrations must not reimport grants.");
        await authentication.RevokeAsync("migration-session");
        Assert.IsNull(await authentication.GetActiveSessionAsync("migration-session", DateTime.UtcNow));
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task ConcurrentRoleChangesAreSerializedAndFailedWritesRollbackRevision(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuTestDriver(db);
        var role = Guid.NewGuid();
        var ids = (await repository.GetMenus()).Select(m => m.Id).ToList();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
        {
            using var writer = fixture.Open();
            Assert.IsTrue((await new MenuTestDriver(writer).SetRoleMenus(role, new() { ids[i % ids.Count] }, RoleMenuChange.Add, "test")));
        })));
        Assert.AreEqual(ids.Count, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
        Assert.AreEqual(9L, await repository.GetPermissionRevisionAsync());
        var before = await repository.GetPermissionRevisionAsync();
        Assert.IsFalse((await repository.SetRoleMenus(role, new() { int.MaxValue }, RoleMenuChange.Replace, "test")));
        Assert.AreEqual(before, await repository.GetPermissionRevisionAsync(), "Rejected data must roll back the revision as well.");
        Assert.AreEqual(ids.Count, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
        await Task.WhenAll(ids.Select(id => Task.Run(async () =>
        {
            using var writer = fixture.Open();
            Assert.IsTrue((await new MenuTestDriver(writer).SetRoleMenus(role, new() { id }, RoleMenuChange.Replace, "test")));
        })));
        Assert.AreEqual(1, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted), "Concurrent replacement must not merge stale snapshots.");
        Assert.IsTrue((await repository.SetRoleMenus(role, new(), RoleMenuChange.Replace, "test")));
        Assert.AreEqual(0, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task ConcurrentOrderingKeepsEverySiblingAndUniquePositions(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var ids = (await new MenuTestDriver(db).GetMenus()).Select(m => m.Id).ToArray();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
        {
            using var writer = fixture.Open();
            var command = i % 2 == 0 ? new MenuSortModel { CurrentId = ids[0], PrevId = ids[2] }
                : new MenuSortModel { CurrentId = ids[2], NextId = ids[1] };
            Assert.IsTrue((await new MenuTestDriver(writer).Move(command, "test")));
        })));
        var menus = (await new MenuTestDriver(db).GetMenus());
        CollectionAssert.AreEquivalent(ids, menus.Select(m => m.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, menus.Select(m => m.Number).ToArray());
        Assert.IsTrue(menus.All(m => m.ParentId == 0));
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task UpdatesKeepOmittedFieldsClearExplicitFieldsAndPreserveTranslations(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuTestDriver(db);
        var menu = (await repository.GetMenus()).First();
        Assert.AreEqual(1, (await repository.Update(new EditMenu { Id = menu.Id, DisplayName = "菜单", ClearFields = new() { "Description" } }, "zh-CN", "editor")));
        var updated = (await repository.Get(menu.Id));
        Assert.IsNull(updated.Description);
        Assert.AreEqual(menu.Icon, updated.Icon);
        Assert.AreEqual("English", updated.DisplayNames.Values["en-US"]);
        Assert.AreEqual("菜单", updated.DisplayNames.Values["zh-CN"]);
        var revision = await repository.GetPermissionRevisionAsync();
        Assert.AreEqual(0, (await repository.Update(new EditMenu { Id = menu.Id, ClearFields = new() { "CreatedBy" } }, "en-US", "editor")));
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
        Assert.AreEqual(0, (await repository.Update(new EditMenu { Id = menu.Id, ParentId = menu.Id }, "en-US", "editor")));
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task UnexpectedDatabaseFailureRollsBackDeletedRelationsAndRevision(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuTestDriver(db);
        var role = Guid.NewGuid();
        var ids = (await repository.GetMenus()).Select(m => m.Id).ToArray();
        Assert.IsTrue((await repository.SetRoleMenus(role, new() { ids[0] }, RoleMenuChange.Replace, "test")));
        var revision = await repository.GetPermissionRevisionAsync();
        if (providerName == "PostgreSQL")
            db.Execute("""
                CREATE FUNCTION reject_test_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Injected write failure'; END $$;
                CREATE TRIGGER reject_test_insert BEFORE INSERT ON public."RoleMenu"
                FOR EACH ROW EXECUTE FUNCTION reject_test_insert();
                """);
        else
            db.Execute("""
                CREATE TRIGGER reject_test_insert ON dbo.RoleMenu AFTER INSERT AS
                BEGIN THROW 51000, 'Injected write failure', 1; END
                """);
        await Assert.ThrowsAsync<DbException>(async () => { await repository.SetRoleMenus(role, new() { ids[1] }, RoleMenuChange.Replace, "test"); });
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
        CollectionAssert.AreEqual(new[] { ids[0] }, db.RoleMenu.Where(m => m.RoleId == role && !m.IsDeleted).Select(m => m.MenuId).ToArray());
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task ApplicationFailureRollsBackAndWriteSessionCannotEscapeTransaction(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuRepository(db);
        var before = (await repository.GetMenus()).First();
        IAccessControlWriteSession captured = null;
        var result = (await repository.Execute(async session =>
        {
            captured = session;
            var menu = (await session.LoadMenus()).First(m => m.Id == before.Id);
            menu.DisplayName = "must roll back";
            Assert.AreEqual(1, (await session.Update(menu)));
            return FluentResults.Result.Fail("application rejected subsequent operation");
        }));
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(before.DisplayName, (await repository.Get(before.Id)).DisplayName);
        Assert.AreEqual(1L, await new PermissionStore(db).GetPermissionRevisionAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await captured.LoadMenus(); });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await repository.Execute(_ => repository.Execute(_ => Task.FromResult(FluentResults.Result.Ok()))); });
        Assert.AreEqual(1L, await new PermissionStore(db).GetPermissionRevisionAsync());
    }

    // 使用真实的事务适配器来执行应用程序用例。
    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task CombinedRoleAccessRollsBackBothGrantsAndRevisionOnFailure(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var transaction = new MenuRepository(db);
        var useCase = new RoleAccessAdministration(transaction, new RoleMenuMutations(transaction, TimeProvider.System),
            new PermissionMutations(), new CurrentUser());
        var role = Guid.NewGuid();
        var ids = db.Menus.Select(m => m.Id).ToArray();
        Assert.IsTrue((await useCase.ReplaceAsync(role, new() { ids[0] }, new() { "menu.add" })).IsSuccess);
        var revision = await new PermissionStore(db).GetPermissionRevisionAsync();
        Assert.AreEqual(2L, revision, "Combined save increments the revision only once.");
        Assert.IsTrue((await useCase.ReplaceAsync(role, new() { ids[1] }, new() { "unknown" })).IsFailed);
        AssertUnchanged();
        if (providerName == "PostgreSQL")
            db.Execute("""
                CREATE FUNCTION reject_permission_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Injected permission failure'; END $$;
                CREATE TRIGGER reject_permission_insert BEFORE INSERT ON public."RolePermissions"
                FOR EACH ROW EXECUTE FUNCTION reject_permission_insert();
                """);
        else
            db.Execute("""
                CREATE TRIGGER reject_permission_insert ON dbo.RolePermissions AFTER INSERT AS
                BEGIN THROW 51000, 'Injected permission failure', 1; END
                """);
        await Assert.ThrowsAsync<DbException>(() => useCase.ReplaceAsync(role, new() { ids[1] }, new() { "menu.edit" }));
        AssertUnchanged();
        void AssertUnchanged()
        {
            CollectionAssert.AreEqual(new[] { ids[0] }, db.RoleMenu.Where(m => m.RoleId == role && !m.IsDeleted).Select(m => m.MenuId).ToArray());
            CollectionAssert.AreEqual(new[] { "menu.add" }, db.GetTable<RolePermission>().Where(p => p.RoleId == role).Select(p => p.Code).ToArray());
            Assert.AreEqual(revision, db.GetTable<PermissionRevision>().Single().Version);
        }
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task SchemaReadinessRequiresMigrationJournalAndMatchingChecksums(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var provider = DatabaseConfiguration.ParseProvider(providerName);
        var health = new My.XXX.Persistences.Health.MigrationSchemaHealthCheck(fixture.ConnectionString, provider);
        var context = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext();
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, (await health.CheckHealthAsync(context)).Status);
        var table = providerName == "PostgreSQL" ? "public.\"SchemaMigrations\"" : "dbo.SchemaMigrations";
        db.Execute($"UPDATE {table} SET checksum = 'changed'");
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, (await health.CheckHealthAsync(context)).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => My.XXX.Persistences.Migrations.MigrationRunner.ApplyAsync(fixture.ConnectionString, provider));
        db.Execute($"DELETE FROM {table}");
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, (await health.CheckHealthAsync(context)).Status);
        await My.XXX.Persistences.Migrations.MigrationRunner.ApplyAsync(fixture.ConnectionString, provider);
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, (await health.CheckHealthAsync(context)).Status);
        db.Execute($"INSERT INTO {table} (name, checksum) VALUES ('999_additive_future_migration', 'future')");
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, (await health.CheckHealthAsync(context)).Status);
        db.Execute($"DROP TABLE {table}");
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, (await health.CheckHealthAsync(context)).Status);
    }

    private sealed class CurrentUser : My.XXX.Services.Abstractions.Interfaces.ICurrentUser
    {
        public UserInfo User => new() { UserId = "editor" };
    }

    private sealed class MenuTestDriver(DBContext db)
    {
        private readonly MenuRepository reads = new(db);
        private readonly MenuMutations writes = new(new MenuRepository(db), TimeProvider.System);
        private readonly RoleMenuMutations assignments = new(new MenuRepository(db), TimeProvider.System);
        public async Task<System.Collections.Generic.List<My.XXX.Services.Menus.Models.MenuState>> GetMenus() => (await reads.GetMenus());
        public async Task<My.XXX.Services.Menus.Models.MenuState> Get(int id) => (await reads.Get(id));
        public Task<long> GetPermissionRevisionAsync() => new PermissionStore(db).GetPermissionRevisionAsync();
        public async Task<bool> SetRoleMenus(Guid role, System.Collections.Generic.List<int> ids, RoleMenuChange change, string user) =>
            (await assignments.SetRoleMenus(role, ids, change, user)).IsSuccess;
        public async Task<bool> Move(MenuSortModel model, string user) => (await writes.Move(model, user)).IsSuccess;
        public async Task<int> Update(EditMenu model, string culture, string user) => (await writes.Update(model, culture, user)).IsSuccess ? 1 : 0;
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string adminConnection, database;
        private readonly DatabaseProvider provider;
        public string ConnectionString { get; }
        private Fixture(string admin, string name, DatabaseProvider type)
        {
            adminConnection = admin; database = name; provider = type;
            var builder = new DbConnectionStringBuilder { ConnectionString = admin };
            builder["Database"] = name;
            ConnectionString = builder.ConnectionString;
        }
        public DBContext Open() => new(new DataOptions<DBContext>(DatabaseConfiguration.Configure(new DataOptions(), ConnectionString, provider)));
        public static async Task<Fixture> Create(string providerName)
        {
            var variable = providerName == "PostgreSQL" ? "ARCH_TEST_POSTGRES" : "ARCH_TEST_SQLSERVER";
            var admin = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(admin))
            {
                if (Environment.GetEnvironmentVariable("ARCH_TEST_REQUIRE_DATABASES") == "1")
                    Assert.Fail($"CI requires {variable}.");
                Assert.Inconclusive($"Set {variable} to a disposable server connection with CREATE DATABASE permission.");
            }
            var fixture = new Fixture(admin, "architecture_test_" + Guid.NewGuid().ToString("N"), DatabaseConfiguration.ParseProvider(providerName));
            await using (var connection = DatabaseConfiguration.CreateConnection(admin, fixture.provider))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE DATABASE " + fixture.database;
                await command.ExecuteNonQueryAsync();
            }
            try
            {
                using var db = fixture.Open();
                db.CreateTable<Menus>();
                db.CreateTable<RoleMenu>();
                await My.XXX.Persistences.Migrations.MigrationRunner.ApplyAsync(fixture.ConnectionString, fixture.provider);
                await My.XXX.Persistences.Migrations.MigrationRunner.ApplyAsync(fixture.ConnectionString, fixture.provider);
                for (var i = 0; i < 3; i++) db.Insert(new Menus
                {
                    DisplayName = "English",
                    DisplayNames = "{\"en-US\":\"English\"}",
                    Description = "description",
                    Icon = "icon",
                    Number = i,
                    ParentId = 0,
                    CreatedBy = "test",
                    CreatedTime = DateTime.Now,
                    IsDisplay = true
                });
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync()
        {
            // 只删除上面创建的随机命名数据库。
            await using var connection = DatabaseConfiguration.CreateConnection(adminConnection, provider);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = provider == DatabaseProvider.PostgreSQL
                ? "DROP DATABASE " + database + " WITH (FORCE)"
                : "ALTER DATABASE " + database + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE " + database;
            await command.ExecuteNonQueryAsync();
        }
    }
}
