using LinqToDB;
using LinqToDB.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Persistence;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Persistence.Repositories;
using My.XXX.Service.DTOs;
using My.XXX.Service.Ports;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace My.XXX.IntegrationTests;

/// <summary>Requires an explicitly supplied disposable database server; each case creates its own database.</summary>
[TestClass]
public class MenuConcurrencyTests
{
    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task ConcurrentRoleChangesAreSerializedAndFailedWritesRollbackRevision(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuRepository(db);
        var role = Guid.NewGuid();
        var ids = repository.GetMenus().Select(m => m.Id).ToList();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            using var writer = fixture.Open();
            Assert.IsTrue(new MenuRepository(writer).SetRoleMenus(role, new() { ids[i % ids.Count] }, RoleMenuChange.Add, "test"));
        })));
        Assert.AreEqual(ids.Count, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
        Assert.AreEqual(8L, await repository.GetPermissionRevisionAsync());
        var before = await repository.GetPermissionRevisionAsync();
        Assert.IsFalse(repository.SetRoleMenus(role, new() { int.MaxValue }, RoleMenuChange.Replace, "test"));
        Assert.AreEqual(before, await repository.GetPermissionRevisionAsync(), "Rejected data must roll back the revision as well.");
        Assert.AreEqual(ids.Count, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
        await Task.WhenAll(ids.Select(id => Task.Run(() =>
        {
            using var writer = fixture.Open();
            Assert.IsTrue(new MenuRepository(writer).SetRoleMenus(role, new() { id }, RoleMenuChange.Replace, "test"));
        })));
        Assert.AreEqual(1, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted), "Concurrent replacement must not merge stale snapshots.");
        Assert.IsTrue(repository.SetRoleMenus(role, new(), RoleMenuChange.Replace, "test"));
        Assert.AreEqual(0, db.RoleMenu.Count(m => m.RoleId == role && !m.IsDeleted));
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task ConcurrentOrderingKeepsEverySiblingAndUniquePositions(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var ids = new MenuRepository(db).GetMenus().Select(m => m.Id).ToArray();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            using var writer = fixture.Open();
            var command = i % 2 == 0 ? new MenuSortModel { CurrentId = ids[0], PrevId = ids[2] }
                : new MenuSortModel { CurrentId = ids[2], NextId = ids[1] };
            Assert.IsTrue(new MenuRepository(writer).Move(command, "test"));
        })));
        var menus = new MenuRepository(db).GetMenus();
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
        var repository = new MenuRepository(db);
        var menu = repository.GetMenus().First();
        Assert.AreEqual(1, repository.Update(new EditMenu { Id = menu.Id, DisplayName = "菜单", ClearFields = new() { "Description" } }, "zh-CN", "editor"));
        var updated = repository.Get(menu.Id);
        Assert.IsNull(updated.Description);
        Assert.AreEqual(menu.Icon, updated.Icon);
        StringAssert.Contains(updated.DisplayNames, "English");
        StringAssert.Contains(updated.DisplayNames, "菜单");
        var revision = await repository.GetPermissionRevisionAsync();
        Assert.AreEqual(0, repository.Update(new EditMenu { Id = menu.Id, ClearFields = new() { "CreatedBy" } }, "en-US", "editor"));
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
        Assert.AreEqual(0, repository.Update(new EditMenu { Id = menu.Id, ParentId = menu.Id }, "en-US", "editor"));
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
    }

    [TestMethod]
    [DataRow("PostgreSQL")]
    [DataRow("SqlServer")]
    public async Task UnexpectedDatabaseFailureRollsBackDeletedRelationsAndRevision(string providerName)
    {
        await using var fixture = await Fixture.Create(providerName);
        using var db = fixture.Open();
        var repository = new MenuRepository(db);
        var role = Guid.NewGuid();
        var ids = repository.GetMenus().Select(m => m.Id).ToArray();
        Assert.IsTrue(repository.SetRoleMenus(role, new() { ids[0] }, RoleMenuChange.Replace, "test"));
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
        Assert.Throws<DbException>(() => repository.SetRoleMenus(role, new() { ids[1] }, RoleMenuChange.Replace, "test"));
        Assert.AreEqual(revision, await repository.GetPermissionRevisionAsync());
        CollectionAssert.AreEqual(new[] { ids[0] }, db.RoleMenu.Where(m => m.RoleId == role && !m.IsDeleted).Select(m => m.MenuId).ToArray());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string adminConnection, database;
        private readonly DatabaseProvider provider;
        private string ConnectionString { get; }
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
            if (string.IsNullOrWhiteSpace(admin)) Assert.Inconclusive($"Set {variable} to a disposable server connection with CREATE DATABASE permission.");
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
                var root = new DirectoryInfo(AppContext.BaseDirectory);
                while (root != null && !File.Exists(Path.Combine(root.FullName, "MyXXXSolution.sln"))) root = root.Parent;
                var suffix = providerName == "PostgreSQL" ? "postgresql" : "sqlserver";
                var sql = File.ReadAllText(Path.Combine(root.FullName, "My.XXX.Persistence", "Migrations", $"001_permission_revision.{suffix}.sql"));
                db.Execute(sql);
                db.Execute(sql); // Upgrades must be safely repeatable.
                for (var i = 0; i < 3; i++) db.Insert(new Menus
                {
                    DisplayName = "English", DisplayNames = "{\"en-US\":\"English\"}", Description = "description", Icon = "icon",
                    Number = i, ParentId = 0, CreatedBy = "test", CreatedTime = DateTime.Now, IsDisplay = true
                });
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync()
        {
            // Only the randomly named database created above is removed.
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
