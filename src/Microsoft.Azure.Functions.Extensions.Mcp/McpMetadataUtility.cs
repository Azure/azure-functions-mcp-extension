// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Utility methods for working with MCP metadata.
/// </summary>
internal static class McpMetadataUtility
{
    /// <summary>
    /// Converts a collection of key-value pairs to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="metadata">The metadata collection to convert.</param>
    /// <returns>A <see cref="JsonObject"/> containing the metadata, or <see langword="null"/> if the collection is empty.</returns>
    public static JsonObject? ToJsonObject(IEnumerable<KeyValuePair<string, object?>>? metadata)
    {
        if (metadata == null)
        {
            return null;
        }

        var jsonObject = new JsonObject();
        foreach (var kvp in metadata)
        {
            jsonObject[kvp.Key] = ConvertToJsonNode(kvp.Value);
        }

        return jsonObject.Count > 0 ? jsonObject : null;
    }

    /// <summary>
    /// Converts an object value to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A <see cref="JsonNode"/> representation of the value.</returns>
    private static JsonNode? ConvertToJsonNode(object? value)
    {
        if (value == null)
        {
            return null;
        }

        // If it's already a JsonNode, use it directly
        if (value is JsonNode jsonNode)
        {
            return jsonNode;
        }

        // Try to parse as JSON string (for complex objects/arrays)
        if (value is string str)
        {
            var trimmed = str.TrimStart();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                try
                {
                    return JsonNode.Parse(str);
                }
                catch
                {
                    // Not valid JSON, treat as regular string
                    return JsonValue.Create(str);
                }
            }
        }

        // For primitives and simple objects, use JsonValue.Create
        return JsonValue.Create(value);
    }
}
