// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public static class TypeExtensions
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
        if (type == typeof(string))
            return false;

        if (type.IsAbstract || type.IsInterface)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        if (!type.IsClass)
            return false;

        if (type.ContainsGenericParameters)
            return false;

        if (type.GetConstructor(Type.EmptyTypes) == null)
            return false;

        return true;
    }

    /// <summary>
    /// Maps a .NET type to a tool property type.
    /// This is used to determine how the property should be represented in the tool's metadata.
    /// </summary>
    /// <returns>A string representing the tool property type.</returns>
    public static string MapToToolPropertyType(this Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(int?)) return "int";
        if (type == typeof(bool) || type == typeof(bool?)) return "bool";
        if (type == typeof(double) || type == typeof(double?)) return "double";
        return "object";
    }
}
