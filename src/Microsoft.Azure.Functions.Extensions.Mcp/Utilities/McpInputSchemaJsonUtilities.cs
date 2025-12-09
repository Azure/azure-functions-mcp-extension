// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Utilities for working with MCP tool JSON schemas.
/// </summary>
public static class McpInputSchemaJsonUtilities
{
    /// <summary>
    /// Validates whether a JsonDocument represents a valid MCP tool input JSON schema.
    /// </summary>
    /// <param name="schema">The JsonDocument to validate.</param>
    /// <returns>true if the schema is valid; otherwise, false.</returns>
    public static bool IsValidMcpToolSchema(JsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var rootElement = schema.RootElement;

        // A valid MCP tool schema should be an object
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Must have a "type" property with value "object"
        if (!rootElement.TryGetProperty("type", out var typeProperty) || 
            typeProperty.ValueKind != JsonValueKind.String ||
            typeProperty.GetString() != "object")
        {
            return false;
        }

        // If "properties" exists, it should be an object
        if (rootElement.TryGetProperty("properties", out var propertiesProperty) && 
            propertiesProperty.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // If "required" exists, it should be an array
        if (rootElement.TryGetProperty("required", out var requiredProperty) && 
            requiredProperty.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts the required property names from a JsonDocument schema.
    /// </summary>
    /// <param name="schema">The schema JsonDocument.</param>
    /// <returns>An array of required property names.</returns>
    public static string[] GetRequiredProperties(JsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var rootElement = schema.RootElement;

        if (!rootElement.TryGetProperty("required", out var requiredProperty) || 
            requiredProperty.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var requiredList = new List<string>();
        foreach (var item in requiredProperty.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is string propertyName)
            {
                requiredList.Add(propertyName);
            }
        }

        return requiredList.ToArray();
    }

    /// <summary>
    /// Creates a JsonElement from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A JsonElement representing the parsed JSON.</returns>
    public static JsonElement CreateFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Checks if inputSchema is default provided schema or worker provided schema
    /// </summary>
    /// <param name="schema"></param>
    /// <returns>True if default provided schema, false if worker provided schema</returns>
    public static bool IsDefaultSchema(JsonElement schema)
    {
        // Check if this is the default empty schema
        if (!schema.TryGetProperty("properties", out var properties) ||
            !schema.TryGetProperty("required", out var required))
        {
            return false;
        }

        // Default schema has empty properties and required arrays
        return properties.ValueKind == JsonValueKind.Object &&
               properties.EnumerateObject().Count() == 0 &&
               required.ValueKind == JsonValueKind.Array &&
               required.GetArrayLength() == 0;
    }
}
