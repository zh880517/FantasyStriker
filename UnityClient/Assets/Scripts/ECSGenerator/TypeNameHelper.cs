using System;
using System.Collections.Generic;
using System.Linq;

public static class TypeNameHelper
{
    private static readonly Dictionary<string, string> builtInTypesToString = new Dictionary<string, string>
    {
        { "System.Boolean", "bool" },
        { "System.Byte", "byte" },
        { "System.SByte", "sbyte" },
        { "System.Char", "char" },
        { "System.Decimal", "decimal" },
        { "System.Double", "double" },
        { "System.Single", "float" },
        { "System.Int32", "int" },
        { "System.UInt32", "uint" },
        { "System.Int64", "long" },
        { "System.UInt64", "ulong" },
        { "System.Object", "object" },
        { "System.Int16", "short" },
        { "System.UInt16", "ushort" },
        { "System.String", "string" },
        { "System.Void", "void" }
    };
    public static string ToCompilableString(this Type type)
    {
        if (builtInTypesToString.TryGetValue(type.FullName, out string name))
        {
            return name;
        }
        if (type.IsGenericType)
        {
            string typeName = type.FullName.Split(new char[] { '`' })[0];
            IEnumerable<Type> genericaArgs = type.GetGenericArguments();
            string[] value = genericaArgs.Select(obj=> ToCompilableString(obj)).ToArray();
            return typeName + "<" + string.Join(", ", value) + ">";
        }
        if (type.IsArray)
        {
            return type.GetElementType().ToCompilableString() + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (type.IsNested)
        {
            return type.FullName.Replace('+', '.');
        }
        return type.FullName;
    }
}
