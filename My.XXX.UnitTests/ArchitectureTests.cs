using static My.XXX.UnitTests.ArchitectureDependencies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Configurations;
using My.XXX.APIs.Controllers;
using My.XXX.APIs.Models;
using My.XXX.Infrastructure.Caching;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authorization.Ports;
using My.XXX.Services.Examples.Policies;
using My.XXX.Services.Menus.Interfaces;
using My.XXX.Services.Menus.Ports;
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
        var serviceRoot = Path.Combine(RepositoryRoot(), "My.XXX.Services");
        var source = Directory.GetFiles(serviceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadLines(path).Select(line => (path, line)));

        var violations = source.Where(x => x.line.Contains("Microsoft.AspNetCore", StringComparison.Ordinal)
            || x.line.Contains("StackExchange.Redis", StringComparison.Ordinal)).ToList();

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
            .Where(type => type.IsInterface && type.Namespace?.EndsWith(".Interfaces", StringComparison.Ordinal) == true);
        foreach (var contract in contracts)
            foreach (var method in contract.GetMethods())
                foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                {
                    var ns = type.Namespace ?? "";
                    Assert.IsFalse(ns.StartsWith("LinqToDB") || ns.StartsWith("My.XXX.Persistences") || ns.StartsWith("Microsoft.AspNetCore") || ns.StartsWith("System.Security.Claims"),
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
                Assert.IsFalse((parameter.ParameterType.Namespace ?? "").StartsWith("My.XXX.Persistences") || (parameter.ParameterType.Namespace?.StartsWith("My.XXX.Services.") == true && parameter.ParameterType.Namespace.EndsWith(".Ports")), controller.Name);
    }

    [TestMethod]
    public void RepositoriesDoNotReturnDeferredQueries()
    {
        foreach (var contract in typeof(IMenuReadRepository).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace?.EndsWith(".Ports", StringComparison.Ordinal) == true))
            foreach (var method in contract.GetMethods())
                Assert.IsFalse(Flatten(method.ReturnType).Any(t => typeof(IQueryable).IsAssignableFrom(t)), method.Name);
    }

    [TestMethod]
    public void ApplicationAssemblyDoesNotReferenceTechnicalImplementations()
    {
        var forbidden = new[] { "linq2db", "Npgsql", "Microsoft.Data.SqlClient", "StackExchange.Redis", "ClosedXML", "My.XXX.Infrastructure", "My.XXX.APIs", "My.XXX.Persistences" };
        foreach (var reference in typeof(IMenuService).Assembly.GetReferencedAssemblies())
            Assert.IsFalse(forbidden.Contains(reference.Name), reference.Name);
    }

    [TestMethod]
    public void ProjectDependencyDirectionsAreExplicit()
    {
        var allowed = new Dictionary<string, string[]>
        {
            ["My.XXX.Shared"] = Array.Empty<string>(),
            ["My.XXX.Contracts"] = Array.Empty<string>(),
            ["My.XXX.Persistences"] = new[] { "My.XXX.Shared", "My.XXX.Contracts", "My.XXX.Services" },
            ["My.XXX.Services"] = new[] { "My.XXX.Shared", "My.XXX.Contracts" },
            ["My.XXX.Infrastructure"] = new[] { "My.XXX.Services" }
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

    [TestMethod]
    public void ApplicationPortsAndTheirModelsAreIndependentOfStorage()
    {
        var visited = new HashSet<Type>();
        void Check(Type type)
        {
            foreach (var part in Flatten(type))
            {
                var ns = part.Namespace ?? "";
                Assert.IsFalse(ns.StartsWith("My.XXX.Persistences") || ns.StartsWith("LinqToDB") || ns.StartsWith("Microsoft.AspNetCore") || ns.StartsWith("System.Security.Claims"), part.FullName);
                if (ns.StartsWith("My.XXX") && visited.Add(part))
                    foreach (var property in part.GetProperties()) Check(property.PropertyType);
            }
        }
        foreach (var port in typeof(IMenuReadRepository).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace?.EndsWith(".Ports", StringComparison.Ordinal) == true))
            foreach (var method in port.GetMethods())
            {
                Check(method.ReturnType);
                foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType);
            }
    }

    [TestMethod]
    public void HttpEntryPointsDoNotUseStorageOrCacheImplementations()
    {
        var assembly = typeof(UserController).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var owner = Owner(type);
            var entry = (typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(owner) &&
                !Attribute.IsDefined(owner, typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute))) ||
                owner.Namespace == "My.XXX.APIs.Common.JWT" ||
                typeof(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata).IsAssignableFrom(owner) ||
                owner == typeof(My.XXX.APIs.Common.Middleware.ExceptionHandlingMiddleware);
            if (!entry) continue;
            foreach (var dependency in Dependencies(type))
            {
                var ns = dependency.Namespace ?? "";
                Assert.IsFalse(ns.StartsWith("My.XXX.Persistences") || ns.StartsWith("LinqToDB") ||
                    ns.StartsWith("StackExchange.Redis") || ns.StartsWith("My.XXX.Infrastructure") ||
                    (ns.StartsWith("My.XXX.Services.") && ns.EndsWith(".Ports")), $"{type}: {dependency}");
            }
        }
    }

    [TestMethod]
    public void MenuPortsDoNotReuseWireModelsAndEndpointsDoNotExposeInternalState()
    {
        var contractsAssembly = typeof(My.XXX.Contracts.DTOs.MenuDto).Assembly;
        foreach (var port in new[] { typeof(IMenuReadRepository), typeof(IAccessControlTransaction), typeof(IAccessControlWriteSession), typeof(IPermissionStore) })
            foreach (var method in port.GetMethods())
                foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                    Assert.AreNotEqual(contractsAssembly, type.Assembly, $"{port.Name}.{method.Name} reuses a wire model.");

        var visited = new HashSet<Type>();
        void Check(Type type)
        {
            foreach (var part in Flatten(type))
            {
                Assert.AreNotEqual(typeof(IMenuService).Assembly, part.Assembly, $"Endpoint exposes application type {part.FullName}");
                if (part.Assembly == contractsAssembly && visited.Add(part))
                    foreach (var property in part.GetProperties()) Check(property.PropertyType);
            }
        }
        foreach (var method in typeof(MenuController).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
        {
            Check(method.ReturnType);
            foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType);
        }
    }

    [TestMethod]
    public void ApplicationUsesNarrowContextsAndTelemetryCannotCarryFrameworkObjects()
    {
        var constructors = new[] { typeof(My.XXX.Services.Menus.MenuCommandService), typeof(My.XXX.Services.Menus.MenuQueryService),
            typeof(My.XXX.Services.Authorization.RoleMenuAssignmentService), typeof(My.XXX.Services.Authentication.AuthenticationService), typeof(My.XXX.Services.Authentication.UserService) }
            .SelectMany(t => t.GetConstructors()).SelectMany(c => c.GetParameters());
        Assert.IsFalse(constructors.Any(p => p.ParameterType.Name == "ICurrentRequest"));
        foreach (var property in typeof(My.XXX.Contracts.DTOs.MetricsInfo).GetProperties())
            Assert.AreNotEqual(typeof(object), property.PropertyType, property.Name);
        var shared = typeof(Paged<>).Assembly;
        Assert.AreNotEqual(shared, typeof(HostingConfig).Assembly);
        Assert.AreNotEqual(shared, typeof(HttpDefaults).Assembly);
        Assert.AreNotEqual(shared, typeof(AppConfig).Assembly);
        Assert.AreNotEqual(shared, typeof(JwtConfig).Assembly);
        Assert.AreNotEqual(shared, typeof(RedisConfig).Assembly);
        Assert.IsFalse(shared.GetTypes().SelectMany(t => t.GetProperties()).Any(p => p.PropertyType == typeof(System.Net.HttpStatusCode)));
        var menuWireTypes = new[] { typeof(My.XXX.Contracts.DTOs.MenuBase), typeof(My.XXX.Contracts.DTOs.MenuBaseDto),
            typeof(My.XXX.Contracts.DTOs.MenuDto), typeof(My.XXX.Contracts.DTOs.MenuSearchPickerDto),
            typeof(My.XXX.Contracts.DTOs.SaveMenu), typeof(My.XXX.Contracts.DTOs.EditMenu) };
        foreach (var method in typeof(My.XXX.Persistences.Mapping.PersistenceMapper).GetMethods())
            foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                Assert.IsFalse(menuWireTypes.Contains(type), $"Persistence mapper exposes {type.Name}");
    }

    [TestMethod]
    public void HostConfigurationAndTechnicalAdaptersHaveExplicitOwners()
    {
        var api = typeof(UserController).Assembly;
        var infrastructure = typeof(My.XXX.Infrastructure.InfrastructureRegistration).Assembly;
        var persistence = typeof(My.XXX.Persistences.PersistenceRegistration).Assembly;
        foreach (var type in new[] { typeof(AppConfig), typeof(JwtConfig), typeof(HostingConfig),
            typeof(HttpDefaults), typeof(PermissionWhitelist), typeof(CultureType), typeof(PolicyType), typeof(BaseResult) })
            Assert.AreEqual(api, type.Assembly, type.FullName);
        foreach (var type in new[] { typeof(RedisConfig), typeof(PermissionDataCache),
            typeof(My.XXX.Infrastructure.Logging.StorageTypeEnum), typeof(My.XXX.Infrastructure.Security.AESHelper),
            typeof(My.XXX.Infrastructure.Health.RedisHealthCheck) })
            Assert.AreEqual(infrastructure, type.Assembly, type.FullName);
        foreach (var type in new[] { typeof(My.XXX.Persistences.Health.SqlHealthCheck),
            typeof(My.XXX.Persistences.Health.PermissionSchemaHealthCheck), typeof(My.XXX.Persistences.Health.AuthenticationSchemaHealthCheck) })
            Assert.AreEqual(persistence, type.Assembly, type.FullName);
        Assert.IsFalse(api.GetTypes().Any(t => typeof(Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck).IsAssignableFrom(t)));
        foreach (var assembly in new[] { api, infrastructure, persistence, typeof(IMenuService).Assembly })
            Assert.IsFalse(assembly.GetTypes().Any(t => (t.Namespace ?? "").StartsWith("My.XXX.Shared")), assembly.FullName);
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
