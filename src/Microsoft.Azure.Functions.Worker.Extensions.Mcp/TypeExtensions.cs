// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

internal static class TypeExtensions
{
    /// <summary>
    /// Checks if the given type qualifies as a POCO for JSON deserialization.
    /// Excludes:
    /// - string
    /// - abstract types and interfaces
    /// - collection types (IEnumerable)
    /// - types without a public parameterless constructor
    /// </summary>
    /// <returns>True if the type is a POCO, otherwise false.</returns>
    public static bool IsPoco(this Type type)
    {
        if (type == typeof(string) || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return false;
        }

        return !typeof(IEnumerable).IsAssignableFrom(type)
           && type.GetConstructor(Type.EmptyTypes) is not null;
    }

    /// <summary>
    /// Maps a .NET type to a tool property type.
    /// This is used to determine how the property should be represented in the tool's metadata.
    /// </summary>
    /// <returns>A string representing the tool property type.</returns>
    public static McpToolPropertyType MapToToolPropertyType(this Type type)
    {
        if (type is null)
        {
            return McpToolPropertyType.Object;
        }

        type = StripNullable(type);

        return type switch
        {
            { } t when t == typeof(string)
                     || t == typeof(DateTime)
                     || t == typeof(DateTimeOffset)
                     || t == typeof(Guid)
                     || t == typeof(char) => McpToolPropertyType.String,

            { } t when t.IsArray || typeof(IEnumerable).IsAssignableFrom(t)
                => MapToToolPropertyType(StripNullable(GetCollectionType(t))).AsArray(),

            { } t when t == typeof(int) => McpToolPropertyType.Integer,
            { } t when t == typeof(bool) => McpToolPropertyType.Boolean,
            { } t when t.IsEnum => McpToolPropertyType.String,
            { } t when IsSupportedNumber(t) => McpToolPropertyType.Number,
            _ => McpToolPropertyType.Object
        };
    }

    private static Type StripNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsSupportedNumber(Type type)
        => Type.GetTypeCode(type) switch
        {
            TypeCode.Byte or
            TypeCode.SByte or
            TypeCode.Int16 or
            TypeCode.UInt16 or
            TypeCode.UInt32 or
            TypeCode.Int64 or
            TypeCode.UInt64 or
            TypeCode.Single or
            TypeCode.Double or
            TypeCode.Decimal => true,
            _ => false,
        };

    private static Type GetCollectionType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType() ?? typeof(object);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        // Any implemented IEnumerable<T>
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var itf = interfaces[i];
            if (itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return itf.GetGenericArguments()[0];
            }
        }

        return typeof(object);
    }
}
