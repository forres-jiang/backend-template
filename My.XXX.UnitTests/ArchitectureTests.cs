using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Controllers;
using My.XXX.Persistence.Interfaces;
using My.XXX.Service.Common;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace My.XXX.UnitTests;

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

    [TestMethod]
    public void ApplicationInterfacesDoNotExposePersistenceOrHttpResponseTypes()
    {
        var contracts = typeof(IMenuService).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == typeof(IMenuService).Namespace);
        foreach (var contract in contracts)
            foreach (var method in contract.GetMethods())
                foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                {
                    var ns = type.Namespace ?? "";
                    Assert.IsFalse(ns.StartsWith("LinqToDB") || ns.StartsWith("My.XXX.Persistence") || ns.StartsWith("Microsoft.AspNetCore"),
                        $"{contract.Name}.{method.Name} exposes {type.FullName}");
                    Assert.IsFalse(typeof(BaseResult).IsAssignableFrom(type), $"{contract.Name} exposes a response DTO");
                }
    }

    [TestMethod]
    public void ControllersDoNotDependOnRepositoriesOrDatabaseContexts()
    {
        foreach (var controller in typeof(UserController).Assembly.GetTypes()
            .Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t)))
            foreach (var parameter in controller.GetConstructors().SelectMany(c => c.GetParameters()))
                Assert.IsFalse((parameter.ParameterType.Namespace ?? "").StartsWith("My.XXX.Persistence"), controller.Name);
    }

    [TestMethod]
    public void RepositoriesDoNotReturnDeferredQueries()
    {
        foreach (var contract in typeof(IMenuRepository).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == typeof(IMenuRepository).Namespace))
            foreach (var method in contract.GetMethods())
                Assert.IsFalse(Flatten(method.ReturnType).Any(t => typeof(IQueryable).IsAssignableFrom(t)), method.Name);
    }

    [TestMethod]
    public void ApplicationAssemblyDoesNotReferenceTechnicalImplementations()
    {
        var forbidden = new[] { "linq2db", "Microsoft.Data.SqlClient", "CSRedisCore", "ClosedXML", "My.XXX.Infrastructure", "My.XXX.APIs" };
        foreach (var reference in typeof(IMenuService).Assembly.GetReferencedAssemblies())
            Assert.IsFalse(forbidden.Contains(reference.Name), reference.Name);
    }

    [TestMethod]
    public void ProjectDependencyDirectionsAreExplicit()
    {
        var allowed = new Dictionary<string, string[]>
        {
            ["My.XXX.Shared"] = Array.Empty<string>(),
            ["My.XXX.Contracts"] = new[] { "My.XXX.Shared" },
            ["My.XXX.Persistence"] = new[] { "My.XXX.Shared", "My.XXX.Contracts" },
            ["My.XXX.Service"] = new[] { "My.XXX.Shared", "My.XXX.Contracts", "My.XXX.Persistence" },
            ["My.XXX.Infrastructure"] = new[] { "My.XXX.Service" }
        };
        foreach (var (project, dependencies) in allowed)
        {
            var file = Directory.GetFiles(Path.Combine(RepositoryRoot(), project), "*.csproj").Single();
            foreach (var reference in XDocument.Load(file).Descendants("ProjectReference"))
            {
                var path = reference.Attribute("Include").Value.Replace('\\', Path.DirectorySeparatorChar);
                var name = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), path)))).Name;
                Assert.IsTrue(dependencies.Contains(name), $"{project} must not depend on {name}");
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.HasElementType)
            foreach (var item in Flatten(type.GetElementType())) yield return item;
        foreach (var argument in type.GetGenericArguments())
            foreach (var item in Flatten(argument)) yield return item;
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
