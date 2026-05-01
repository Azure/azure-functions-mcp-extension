// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;

/// <summary>
/// Extension methods for determining structured content support for types.
/// </summary>
internal static class StructuredContentTypeExtensions
{
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
}
