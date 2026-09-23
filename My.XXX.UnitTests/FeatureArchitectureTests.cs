using static My.XXX.UnitTests.ArchitectureDependencies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace My.XXX.UnitTests;

[TestClass]
public class FeatureArchitectureTests
{
    [TestMethod]
    public void CompatibilityFacadeHasOnlyExplicitlyGrandfatheredConsumers()
    {
        var allowed = new[] { typeof(My.XXX.APIs.Controllers.MenuController),
            typeof(My.XXX.Services.Compatibility.MenuService), typeof(My.XXX.Services.ServiceRegistration),
            typeof(My.XXX.Services.Menus.Interfaces.IMenuService) };
        var assemblies = new[] { typeof(MenuQueryService).Assembly, typeof(My.XXX.APIs.Program).Assembly,
            typeof(My.XXX.Persistences.PersistenceRegistration).Assembly, typeof(My.XXX.Infrastructure.InfrastructureRegistration).Assembly };
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            var owner = type;
            while (owner.DeclaringType != null) owner = owner.DeclaringType;
            if (allowed.Contains(owner)) continue;
            foreach (var dependency in Dependencies(type))
                Assert.IsFalse(dependency == typeof(My.XXX.Services.Menus.Interfaces.IMenuService) ||
                    dependency.Namespace == "My.XXX.Services.Compatibility", $"New compatibility consumer: {type} -> {dependency}");
        }
    }

    [TestMethod]
    public void FeaturesDoNotReachIntoEachOtherOrTheCompatibilityFacade()
    {
        foreach (var type in typeof(MenuQueryService).Assembly.GetTypes())
        {
            var ns = type.Namespace ?? "";
            var menu = ns.StartsWith("My.XXX.Services.Menus");
            var authorization = ns.StartsWith("My.XXX.Services.Authorization");
            if (!menu && !authorization) continue;
            foreach (var dependency in Dependencies(type))
            {
                var target = dependency.Namespace ?? "";
                Assert.IsFalse(target.StartsWith("My.XXX.Services.Compatibility") ||
                    (menu && target.StartsWith("My.XXX.Services.Authorization")) ||
                    (authorization && target.StartsWith("My.XXX.Services.Menus")), $"{type}: {dependency}");
                if ((Owner(type).Namespace ?? "").EndsWith(".Policies") || (Owner(type).Namespace ?? "").EndsWith(".Models"))
                    Assert.IsFalse(target.StartsWith("Newtonsoft") || target.StartsWith("System.Text.Json") ||
                        target.StartsWith("My.XXX.Contracts.Serialization"), $"Policy/model contains serialization: {type}");
            }
        }
    }

    [TestMethod]
    public void StoragePortsCannotAcceptHttpPaginationModels()
    {
        foreach (var port in typeof(MenuQueryService).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace?.EndsWith(".Ports") == true))
            foreach (var parameter in port.GetMethods().SelectMany(m => m.GetParameters()))
                foreach (var type in Flatten(parameter.ParameterType))
                    Assert.IsFalse(typeof(QueryBase).IsAssignableFrom(type), $"{port}: {parameter}");
    }

}
