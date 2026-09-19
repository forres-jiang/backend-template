using LinqToDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Persistence;
using My.XXX.Persistence.PersistantObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.UnitTests;

[TestClass]
public class DatabaseProviderTests
{
    [TestMethod]
    [DataRow("SqlServer", "PostgreSQL")]
    [DataRow("PostgreSQL", "SqlServer")]
    [DataRow("PostgreSQL", "PostgreSQL")]
    [DataRow(null, null)]
    public void ContextsUseIndependentProvidersAndTranslateAllTables(string primary, string mail)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["DatabaseProviders:Default"] = primary,
            ["DatabaseProviders:MailMaster"] = mail,
            ["ConnectionStrings:Default"] = ConnectionString(primary),
            ["ConnectionStrings:MailMaster"] = ConnectionString(mail)
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDBs(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DBContext>();
        var mails = scope.ServiceProvider.GetRequiredService<MailContext>();
        var schema = primary == "PostgreSQL" ? "public." : "[dbo].";
        StringAssert.Contains(db.Menus.Where(x => !x.IsDeleted).Skip(2).Take(5).ToSqlQuery().Sql, schema);
        StringAssert.Contains(db.RoleMenu.ToSqlQuery().Sql, schema);
        StringAssert.Contains(db.Operations.ToSqlQuery().Sql, schema);
        StringAssert.Contains(db.Demo.ToSqlQuery().Sql, "Demo");
        StringAssert.Contains(db.GetTable<DemoDetail>().ToSqlQuery().Sql, "DemoDetail");
        StringAssert.Contains(mails.MailQueues.ToSqlQuery().Sql, mail == "PostgreSQL" ? "public." : "[dbo].");
        StringAssert.Contains(mails.Attachments.ToSqlQuery().Sql, "Attachment");
        StringAssert.Contains(mails.AttachmentMappings.ToSqlQuery().Sql, "AttachmentMapping");
        using var connection = DatabaseConfiguration.CreateConnection(ConnectionString(mail), DatabaseConfiguration.ParseProvider(mail));
        Assert.AreEqual(mail == "PostgreSQL" ? "NpgsqlConnection" : "SqlConnection", connection.GetType().Name);
    }

    [TestMethod]
    public void InvalidProviderIsRejectedAndNamesAreCaseInsensitive()
    {
        Assert.AreEqual(DatabaseProvider.PostgreSQL, DatabaseConfiguration.ParseProvider("postgresql"));
        Assert.AreEqual(DatabaseProvider.SqlServer, DatabaseConfiguration.ParseProvider("sqlserver"));
        Assert.ThrowsExactly<InvalidOperationException>(() => DatabaseConfiguration.ParseProvider("PostgresTypo"));
        Assert.ThrowsExactly<InvalidOperationException>(() => DatabaseConfiguration.ParseProvider(""));
    }

    private static string ConnectionString(string provider) => provider == "PostgreSQL"
        ? "Host=localhost;Database=test;Username=test;Password=test"
        : "Server=localhost;Database=test;Integrated Security=true;TrustServerCertificate=true";
}
