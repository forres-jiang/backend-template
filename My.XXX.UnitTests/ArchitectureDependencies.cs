using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace My.XXX.UnitTests;

/// <summary>Shared dependency reader for signatures, nested generic arguments and executable code.</summary>
internal static class ArchitectureDependencies
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(c => unchecked((ushort)c.Value));

    public static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.HasElementType)
            foreach (var item in Flatten(type.GetElementType())) yield return item;
        foreach (var argument in type.GetGenericArguments())
            foreach (var item in Flatten(argument)) yield return item;
    }

    public static Type Owner(Type type)
    {
        while (type.DeclaringType != null) type = type.DeclaringType;
        return type;
    }

    public static IEnumerable<Type> Dependencies(Type type) => Direct(type).SelectMany(Flatten).Distinct();

    public static IEnumerable<Type> ModelGraph(Type root)
    {
        var pending = new Stack<Type>();
        var visited = new HashSet<Type>();
        pending.Push(root);
        while (pending.Count > 0)
            foreach (var type in Flatten(pending.Pop()))
            {
                if (!visited.Add(type)) continue;
                yield return type;
                if (!(type.Namespace ?? "").StartsWith("My.XXX", StringComparison.Ordinal)) continue;
                if (type.BaseType != null) pending.Push(type.BaseType);
                foreach (var property in type.GetProperties(Flags)) pending.Push(property.PropertyType);
                foreach (var field in type.GetFields(Flags)) pending.Push(field.FieldType);
            }
    }

    private static IEnumerable<Type> MemberTypes(MemberInfo member)
    {
        if (member is Type type) { yield return type; yield break; }
        if (member.DeclaringType != null) yield return member.DeclaringType;
        if (member is FieldInfo field) yield return field.FieldType;
        if (member is MethodBase method)
        {
            foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
            if (method is MethodInfo info) yield return info.ReturnType;
            if (method.IsGenericMethod)
                foreach (var argument in method.GetGenericArguments()) yield return argument;
        }
    }

    private static IEnumerable<Type> Direct(Type type)
    {
        if (type.BaseType != null) yield return type.BaseType;
        foreach (var contract in type.GetInterfaces()) yield return contract;
        foreach (var field in type.GetFields(Flags)) yield return field.FieldType;
        foreach (var property in type.GetProperties(Flags)) yield return property.PropertyType;
        var methods = type.GetMethods(Flags).Cast<MethodBase>().Concat(type.GetConstructors(Flags));
        if (type.TypeInitializer != null) methods = methods.Append(type.TypeInitializer);
        foreach (var method in methods.Distinct())
        {
            foreach (var dependency in MemberTypes(method)) yield return dependency;
            var body = method.GetMethodBody();
            if (body == null) continue;
            foreach (var local in body.LocalVariables) yield return local.LocalType;
            foreach (var handler in body.ExceptionHandlingClauses)
                if (handler.Flags == ExceptionHandlingClauseOptions.Clause && handler.CatchType != null)
                    yield return handler.CatchType;
            var il = body.GetILAsByteArray();
            for (var offset = 0; offset < il.Length;)
            {
                ushort value = il[offset++];
                if (value == 0xfe) value = (ushort)(0xfe00 | il[offset++]);
                var operand = Codes[value].OperandType;
                if (operand is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
                {
                    var member = method.Module.ResolveMember(BitConverter.ToInt32(il, offset),
                        type.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                    foreach (var dependency in MemberTypes(member)) yield return dependency;
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
