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
    /// Determines whether structured content should be created for the given object.
    /// </summary>
    /// <remarks>
    /// <para><b>Type Resolution Rules (evaluated in order):</b></para>
    /// <list type="number">
    ///   <item>
    ///     <term>Direct Attribution</term>
    ///     <description>If the type is decorated with <see cref="McpContentAttribute"/>, returns true.</description>
    ///   </item>
    ///   <item>
    ///     <term>Collection Element Attribution</term>
    ///     <description>If the type is a collection and the element type has <see cref="McpContentAttribute"/>, returns true.
    ///     Nested collections are recursively checked.</description>
    ///   </item>
    ///   <item>
    ///     <term>No Attribution</term>
    ///     <description>Otherwise, returns false (text content only).</description>
    ///   </item>
    /// </list>
    /// 
    /// <para><b>Supported Types:</b></para>
    /// <list type="bullet">
    ///   <item><c>class</c>, <c>record class</c>, <c>struct</c>, <c>record struct</c> - Fully supported</item>
    ///   <item><c>interface</c>, <c>enum</c> - Not supported (cannot be decorated with attributes in a meaningful way)</item>
    /// </list>
    /// 
    /// <para><b>Collection Handling:</b></para>
    /// <list type="bullet">
    ///   <item>Arrays: Element type is checked recursively</item>
    ///   <item>Generic collections (List, IEnumerable, etc.): First type argument is checked recursively</item>
    ///   <item>Dictionaries: Not supported</item>
    ///   <item>Nested collections: Recursively unwrapped until a non-collection element type is found</item>
    /// </list>
    /// 
    /// <para><b>Not Supported:</b></para>
    /// <list type="bullet">
    ///   <item>Inherited attribution: Only direct decoration with <see cref="McpContentAttribute"/> is recognized</item>
    ///   <item>Dictionary types: Any type implementing <c>IEnumerable&lt;KeyValuePair&lt;,&gt;&gt;</c></item>
    /// </list>
    /// </remarks>
    /// <param name="obj">The object to evaluate.</param>
    /// <returns>True if structured content should be created; otherwise, false.</returns>
    public static bool ShouldCreateStructuredContent(this object obj)
    {
        var type = obj.GetType();
        return type.HasMcpResultAttributeRecursive();
    }

    /// <summary>
    /// Recursively checks if the type or its collection element types have the <see cref="McpContentAttribute"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type or its element types have the attribute; otherwise, false.</returns>
    public static bool HasMcpResultAttributeRecursive(this Type type)
    {
        // Check if the type itself is decorated with McpContentAttribute (no inheritance)
        if (type.HasMcpResultAttribute())
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

            if (elementType != null)
            {
                // Recursively check element type (handles nested collections)
                if (elementType.HasMcpResultAttributeRecursive())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the type is directly decorated with <see cref="McpContentAttribute"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has the attribute; otherwise, false.</returns>
    public static bool HasMcpResultAttribute(this Type type)
    {
        // Only check for direct attribution, not inherited
        return type.GetCustomAttributes(typeof(McpContentAttribute), inherit: false).Length > 0;
    }

    /// <summary>
    /// Checks if the type is a dictionary type (implements IEnumerable&lt;KeyValuePair&lt;,&gt;&gt;).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a dictionary type; otherwise, false.</returns>
    public static bool IsDictionaryType(this Type type)
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
    public static Type? GetCollectionElementType(this Type type)
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
