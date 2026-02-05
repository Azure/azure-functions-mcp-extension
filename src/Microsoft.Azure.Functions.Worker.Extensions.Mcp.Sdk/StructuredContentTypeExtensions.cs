// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;

/// <summary>
/// Extension methods for determining structured content support for types.
/// </summary>
internal static class StructuredContentTypeExtensions
{
    /// <summary>
    /// Recursively checks if the type or its collection element types have the <see cref="McpContentAttribute"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type or its element types have the attribute; otherwise, false.</returns>
    internal static bool HasMcpContentAttributeRecursive(this Type type)
    {
        // Check if the type itself is decorated with McpContentAttribute (no inheritance)
        if (type.HasMcpContentAttribute())
        {
            return true;
        }

        // Skip dictionary types entirely
        if (type.IsDictionaryType())
        {
            return false;
        }

        // Check if it's a collection type
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.GetCollectionElementType();

            // Recursively check element type (handles nested collections)
            if (elementType != null && elementType.HasMcpContentAttributeRecursive())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the type is directly decorated with <see cref="McpContentAttribute"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has the attribute; otherwise, false.</returns>
    internal static bool HasMcpContentAttribute(this Type type)
    {
        // Only check for direct attribution, not inherited
        return type.GetCustomAttributes(typeof(McpContentAttribute), inherit: false).Length > 0;
    }

    /// <summary>
    /// Checks if the type is a dictionary type (implements IEnumerable&lt;KeyValuePair&lt;,&gt;&gt;).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a dictionary type; otherwise, false.</returns>
    internal static bool IsDictionaryType(this Type type)
    {
        // Check if the type implements IEnumerable<KeyValuePair<,>>
        // All dictionary types (Dictionary<,>, IDictionary<,>, IReadOnlyDictionary<,>, etc.) implement this
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                      i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                      i.GetGenericArguments()[0].IsGenericType &&
                      i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
    }

    /// <summary>
    /// Gets the element type of a collection type.
    /// </summary>
    /// <param name="type">The collection type.</param>
    /// <returns>The element type, or null if the type is not a supported collection.</returns>
    internal static Type? GetCollectionElementType(this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var genericArgs = type.GetGenericArguments();

        // Handle generic collections - check the first type argument
        return genericArgs.Length > 0 ? genericArgs[0] : null;
    }
}
