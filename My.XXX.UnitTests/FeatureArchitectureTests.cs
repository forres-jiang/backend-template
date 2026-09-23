using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
            foreach (var dependency in Dependencies(type).SelectMany(Flatten))
                Assert.IsFalse(dependency == typeof(My.XXX.Services.Menus.Interfaces.IMenuService) ||
                    dependency.Namespace == "My.XXX.Services.Compatibility", $"New compatibility consumer: {type} -> {dependency}");
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
                if (ns.EndsWith(".Policies") || ns.EndsWith(".Models"))
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
                Assert.IsFalse(typeof(QueryBase).IsAssignableFrom(parameter.ParameterType), $"{port}: {parameter}");
    }

    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(c => unchecked((ushort)c.Value));

    private static IEnumerable<Type> Dependencies(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        if (type.BaseType != null) yield return type.BaseType;
        foreach (var contract in type.GetInterfaces()) yield return contract;
        foreach (var field in type.GetFields(flags)) yield return field.FieldType;
        foreach (var method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
        {
            foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
            if (method is MethodInfo info) yield return info.ReturnType;
            var il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null) continue;
            for (var offset = 0; offset < il.Length;)
            {
                ushort value = il[offset++];
                if (value == 0xfe) value = (ushort)(0xfe00 | il[offset++]);
                var operand = Codes[value].OperandType;
                if (operand is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
                {
                    var member = method.Module.ResolveMember(BitConverter.ToInt32(il, offset), type.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                    var dependency = member as Type ?? member.DeclaringType;
                    if (dependency != null) yield return dependency;
                }
                offset += operand switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                    _ => 4
                };
            }
        }
    }
}
