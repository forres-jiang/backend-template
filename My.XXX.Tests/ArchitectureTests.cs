using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Infra;
using My.XXX.Service.Common;
using System;
using System.IO;
using System.Linq;

namespace My.XXX.Tests;

[TestClass]
public class ArchitectureTests
{
    [TestMethod]
    public void ServiceLayerDoesNotUseAspNetOrStaticRedis()
    {
        var serviceRoot = Path.Combine(RepositoryRoot(), "My.XXX.Service");
        var source = Directory.GetFiles(serviceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadLines(path).Select(line => (path, line)));

        var violations = source.Where(x => x.line.Contains("Microsoft.AspNetCore", StringComparison.Ordinal)
            || x.line.Contains("RedisHelper", StringComparison.Ordinal)).ToList();

        Assert.AreEqual(0, violations.Count,
            string.Join(Environment.NewLine, violations.Select(x => $"{x.path}: {x.line.Trim()}")));
    }

    [TestMethod]
    public void QueryHelperReturnsComposedQuery()
    {
        var values = new[] { new QueryRow(1, "first"), new QueryRow(2, "second") }.AsQueryable();
        var result = QueryHelper.Build(values, new { id = 2 }).ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("second", result[0].Name);
    }

    [TestMethod]
    public void FailureFactoryPreservesRequestedStatusCode()
    {
        var result = MyResult.Fail("conflict", 409);
        Assert.AreEqual(409, result.StatusCode);
        Assert.IsNull(result.Data);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MyXXXSolution.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record QueryRow(int Id, string Name);
}
