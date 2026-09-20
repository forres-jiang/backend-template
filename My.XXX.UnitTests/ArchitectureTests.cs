using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Controllers;
using My.XXX.Service.Common;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
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
                Assert.IsFalse((parameter.ParameterType.Namespace ?? "").StartsWith("My.XXX.Persistence") || parameter.ParameterType.Namespace == "My.XXX.Service.Ports", controller.Name);
    }

    [TestMethod]
    public void RepositoriesDoNotReturnDeferredQueries()
    {
        foreach (var contract in typeof(IMenuReadRepository).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == typeof(IMenuReadRepository).Namespace))
            foreach (var method in contract.GetMethods())
                Assert.IsFalse(Flatten(method.ReturnType).Any(t => typeof(IQueryable).IsAssignableFrom(t)), method.Name);
    }

    [TestMethod]
    public void ApplicationAssemblyDoesNotReferenceTechnicalImplementations()
    {
        var forbidden = new[] { "linq2db", "Npgsql", "Microsoft.Data.SqlClient", "CSRedisCore", "StackExchange.Redis", "ClosedXML", "My.XXX.Infrastructure", "My.XXX.APIs", "My.XXX.Persistence" };
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
            ["My.XXX.Persistence"] = new[] { "My.XXX.Shared", "My.XXX.Contracts", "My.XXX.Service" },
            ["My.XXX.Service"] = new[] { "My.XXX.Shared", "My.XXX.Contracts" },
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

    [TestMethod]
    public void ApplicationPortsAndTheirModelsAreIndependentOfStorage()
    {
        var visited = new HashSet<Type>();
        void Check(Type type)
        {
            foreach (var part in Flatten(type))
            {
                var ns = part.Namespace ?? "";
                Assert.IsFalse(ns.StartsWith("My.XXX.Persistence") || ns.StartsWith("LinqToDB") || ns.StartsWith("Microsoft.AspNetCore"), part.FullName);
                if (ns.StartsWith("My.XXX") && visited.Add(part))
                    foreach (var property in part.GetProperties()) Check(property.PropertyType);
            }
        }
        foreach (var port in typeof(IMenuReadRepository).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace == typeof(IMenuReadRepository).Namespace))
            foreach (var method in port.GetMethods())
            {
                Check(method.ReturnType);
                foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType);
            }
    }

    [TestMethod]
    public void ActiveControllerMethodBodiesDoNotUseStorageOrCacheImplementations()
    {
        var codes = typeof(System.Reflection.Emit.OpCodes).GetFields()
            .Where(f => f.FieldType == typeof(System.Reflection.Emit.OpCode))
            .Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)).ToDictionary(c => unchecked((ushort)c.Value));
        var controllers = typeof(UserController).Assembly.GetTypes().Where(t =>
            typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) &&
            !Attribute.IsDefined(t, typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute)));
        foreach (var controller in controllers)
            foreach (var type in new[] { controller }.Concat(controller.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)))
                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    var il = method.GetMethodBody()?.GetILAsByteArray();
                    if (il == null) continue;
                    for (var offset = 0; offset < il.Length;)
                    {
                        ushort value = il[offset++];
                        if (value == 0xfe) value = (ushort)(0xfe00 | il[offset++]);
                        var operand = codes[value].OperandType;
                        if (operand is System.Reflection.Emit.OperandType.InlineMethod or System.Reflection.Emit.OperandType.InlineField or System.Reflection.Emit.OperandType.InlineType or System.Reflection.Emit.OperandType.InlineTok)
                        {
                            var member = method.Module.ResolveMember(BitConverter.ToInt32(il, offset), type.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                            var ns = (member as Type)?.Namespace ?? member.DeclaringType?.Namespace ?? "";
                            Assert.IsFalse(ns.StartsWith("My.XXX.Persistence") || ns.StartsWith("LinqToDB") || ns.StartsWith("StackExchange.Redis") || ns.StartsWith("My.XXX.Infrastructure") || ns == "My.XXX.Service.Ports", $"{controller.Name}.{method.Name}: {member}");
                        }
                        offset += operand switch
                        {
                            System.Reflection.Emit.OperandType.InlineNone => 0,
                            System.Reflection.Emit.OperandType.ShortInlineBrTarget or System.Reflection.Emit.OperandType.ShortInlineI or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                            System.Reflection.Emit.OperandType.InlineVar => 2,
                            System.Reflection.Emit.OperandType.InlineI8 or System.Reflection.Emit.OperandType.InlineR => 8,
                            System.Reflection.Emit.OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                            _ => 4
                        };
                    }
                }
    }

    [TestMethod]
    public void MenuPortsDoNotReuseWireModelsAndEndpointsDoNotExposeInternalState()
    {
        var contractsAssembly = typeof(My.XXX.Service.DTOs.MenuDto).Assembly;
        foreach (var port in new[] { typeof(IMenuReadRepository), typeof(IMenuTransaction), typeof(IMenuWriteSession), typeof(IPermissionStore) })
            foreach (var method in port.GetMethods())
                foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                    Assert.AreNotEqual(contractsAssembly, type.Assembly, $"{port.Name}.{method.Name} reuses a wire model.");

        var visited = new HashSet<Type>();
        void Check(Type type)
        {
            foreach (var part in Flatten(type))
            {
                Assert.IsFalse((part.Namespace ?? "").StartsWith("My.XXX.Service.Models"), part.FullName);
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
        var constructors = new[] { typeof(My.XXX.Service.MenuCommandService), typeof(My.XXX.Service.MenuQueryService),
            typeof(My.XXX.Service.RolePermissionService), typeof(My.XXX.Service.AuthenticationService) }
            .SelectMany(t => t.GetConstructors()).SelectMany(c => c.GetParameters());
        Assert.IsFalse(constructors.Any(p => p.ParameterType == typeof(ICurrentRequest)));
        foreach (var property in typeof(My.XXX.Service.DTOs.MetricsInfo).GetProperties())
            Assert.AreNotEqual(typeof(object), property.PropertyType, property.Name);
        var shared = typeof(Paged<>).Assembly;
        Assert.AreNotEqual(shared, typeof(AppConfig).Assembly);
        Assert.AreNotEqual(shared, typeof(JwtConfig).Assembly);
        Assert.AreNotEqual(shared, typeof(RedisConfig).Assembly);
        Assert.IsFalse(shared.GetTypes().SelectMany(t => t.GetProperties()).Any(p => p.PropertyType == typeof(System.Net.HttpStatusCode)));
        var menuWireTypes = new[] { typeof(My.XXX.Service.DTOs.MenuBase), typeof(My.XXX.Service.DTOs.MenuBaseDto),
            typeof(My.XXX.Service.DTOs.MenuDto), typeof(My.XXX.Service.DTOs.MenuSearchPickerDto),
            typeof(My.XXX.Service.DTOs.SaveMenu), typeof(My.XXX.Service.DTOs.EditMenu) };
        foreach (var method in typeof(My.XXX.Persistence.Mapping.PersistenceMapper).GetMethods())
            foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType).SelectMany(Flatten))
                Assert.IsFalse(menuWireTypes.Contains(type), $"Persistence mapper exposes {type.Name}");
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
