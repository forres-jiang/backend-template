using ClosedXML.Excel;
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Infrastructure;
using My.XXX.Persistences;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Validators;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace My.XXX.IntegrationTests;

[TestClass]
public class ComponentCompatibilityTests
{
    [TestMethod]
    public void ExcelExportPreservesChineseText()
    {
        var table = new DataTable();
        table.Columns.Add("名称");
        table.Rows.Add("升级验证");

        using var stream = ExcelHelper.DataTableToExcel(table, "数据");
        Assert.IsNotNull(stream);
        Assert.AreEqual(0L, stream.Position);
        using var workbook = new XLWorkbook(stream);
        Assert.AreEqual("名称", workbook.Worksheet("数据").Cell(1, 1).GetString());
        Assert.AreEqual("升级验证", workbook.Worksheet("数据").Cell(2, 1).GetString());
    }

    [TestMethod]
    public void TableExportKeepsValuesAsText()
    {
        var table = new DataTable();
        table.Columns.Add("编号");
        table.Columns.Add("文本");
        table.Columns.Add("数量", typeof(int));
        table.Columns.Add("空值");
        table.Rows.Add("00123", "=1+1", 42, System.DBNull.Value);
        using var stream = ExcelHelper.DataTableToExcel(table, "数据");
        Assert.IsNotNull(stream);
        Assert.IsTrue(stream.CanRead && stream.CanSeek);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.AreEqual("00123", sheet.Cell(2, 1).GetString());
        Assert.AreEqual("=1+1", sheet.Cell(2, 2).GetString());
        Assert.IsFalse(sheet.Cell(2, 2).HasFormula);
        Assert.AreEqual(XLDataType.Text, sheet.Cell(2, 3).DataType);
        Assert.AreEqual("42", sheet.Cell(2, 3).GetString());
        Assert.AreEqual("", sheet.Cell(2, 4).GetString());
    }

    [TestMethod]
    public void ListExportUsesMappedColumnOrderAndDefaultSheetName()
    {
        var rows = new List<DemoModel>
        {
            new DemoModel { Id = 7, DemoString = "00123", DemoInt = 42 },
            new DemoModel { Id = 8, DemoString = null, DemoInt = 3 }
        };
        var columns = new Dictionary<string, string>
        {
            ["DemoInt"] = "数量",
            ["DemoString"] = "编号",
            ["Unknown"] = "未映射"
        };
        using var stream = ExcelHelper.ListToExport(null, rows, columns);
        Assert.IsNotNull(stream);
        Assert.AreEqual(0L, stream.Position);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Sheet1");
        Assert.AreEqual("数量", sheet.Cell(1, 1).GetString());
        Assert.AreEqual("编号", sheet.Cell(1, 2).GetString());
        Assert.AreEqual("未映射", sheet.Cell(1, 3).GetString());
        Assert.AreEqual("42", sheet.Cell(2, 1).GetString());
        Assert.AreEqual("00123", sheet.Cell(2, 2).GetString());
        Assert.AreEqual("", sheet.Cell(2, 3).GetString());
        Assert.AreEqual("3", sheet.Cell(3, 1).GetString());
        Assert.AreEqual("", sheet.Cell(3, 2).GetString());
        Assert.IsTrue(sheet.Cell(1, 4).IsEmpty());
    }

    [TestMethod]
    public void EmptyExportsKeepExistingReturnContract()
    {
        using var nullTable = ExcelHelper.DataTableToExcel(null, "数据");
        using var emptyTable = ExcelHelper.DataTableToExcel(new DataTable(), "数据");
        Assert.IsNotNull(nullTable);
        Assert.IsNotNull(emptyTable);
        Assert.AreEqual(0L, nullTable.Length);
        Assert.AreEqual(0L, emptyTable.Length);
        Assert.IsNull(ExcelHelper.ListToExport<DemoModel>(null, null, null));
        Assert.IsNull(ExcelHelper.ListToExport("数据", new List<DemoModel>(), null));
    }

    [TestMethod]
    public void InvalidWorksheetNameReturnsNull()
    {
        var table = new DataTable();
        table.Columns.Add("Name");
        table.Rows.Add("value");
        Assert.IsNull(ExcelHelper.DataTableToExcel(table, "invalid/name"));
        Assert.IsNull(ExcelHelper.ListToExport("invalid/name",
            new List<DemoModel> { new DemoModel() },
            new Dictionary<string, string> { ["Id"] = "编号" }));
    }

    [TestMethod]
    public void ValidatorRejectsMissingFieldsAndAcceptsValidInput()
    {
        var validator = new DemoValidator();
        Assert.IsFalse(validator.Validate(new DemoModel()).IsValid);
        Assert.IsTrue(validator.Validate(new DemoModel { DemoString = "demo", DemoInt = 1 }).IsValid);
    }

    [TestMethod]
    public void SqlServerQueryCanBeTranslatedWithoutConnecting()
    {
        var options = new DataOptions().UseSqlServer(
            "Server=localhost;Database=CompatibilityTest;Integrated Security=true;TrustServerCertificate=true",
            SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient);
        using var db = new DBContext(new DataOptions<DBContext>(options));
        var sql = db.Demo.Where(demo => demo.Id == 42).ToSqlQuery().Sql;
        StringAssert.Contains(sql, "Demo");
        StringAssert.Contains(sql, "Id");
    }

    [TestMethod]
    public void SwaggerKeepsBearerAuthenticationRequirement()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["JwtConfig:Secret"] = "compatibility-test-secret-at-least-32-characters",
            ["JwtConfig:Issuer"] = "test",
            ["JwtConfig:Audience"] = "test",
            ["AllowedHostArray:0"] = "https://localhost"
        });
        builder.Services.AddDefaultService(builder.Configuration);
        using var provider = builder.Services.BuildServiceProvider();
        var document = provider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.AreEqual("bearer", document.Components.SecuritySchemes["bearerAuth"].Scheme);
        Assert.IsTrue(document.Security.Any(requirement =>
            requirement.Keys.Any(scheme => scheme.Reference.Id == "bearerAuth")));
    }
}
