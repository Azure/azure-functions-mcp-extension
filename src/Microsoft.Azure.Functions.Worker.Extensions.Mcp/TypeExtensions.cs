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
    public static string MapToToolPropertyType(this Type type) => type switch
    {
        { } when type == typeof(string) => "string",
        { } when typeof(IEnumerable).IsAssignableFrom(type) => "array",
        { } when type == typeof(int) || type == typeof(int?) => "integer",
        { } when type == typeof(bool) || type == typeof(bool?) => "boolean",
        { } when type == typeof(double) || type == typeof(double?) => "number",
        _ => "object"
    };
}
