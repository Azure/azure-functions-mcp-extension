// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Utilities for working with MCP tool JSON schemas.
/// </summary>
internal static class McpJsonUtilities
{
    /// <summary>
    /// Gets the default MCP tool schema representing an object with no properties or required fields.
    /// </summary>
    public static readonly JsonElement DefaultMcpToolSchema = CreateDefaultSchema();

    /// <summary>
    /// Validates whether a JsonElement represents a valid MCP tool input JSON schema.
    /// </summary>
    /// <param name="schema">The JsonElement to validate.</param>
    /// <returns>true if the schema is valid; otherwise, false.</returns>
    public static bool IsValidMcpToolSchema(JsonElement schema)
    {
        // A valid MCP tool schema should be an object
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Must have a "type" property with value "object"
        if (!schema.TryGetProperty("type", out var typeProperty) || 
            typeProperty.ValueKind != JsonValueKind.String ||
            typeProperty.GetString() != "object")
        {
            return false;
        }

        // If "properties" exists, it should be an object
        if (schema.TryGetProperty("properties", out var propertiesProperty) && 
            propertiesProperty.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // If "required" exists, it should be an array
        if (schema.TryGetProperty("required", out var requiredProperty) && 
            requiredProperty.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates the default MCP tool schema.
    /// </summary>
    /// <returns>A JsonElement representing the default schema.</returns>
    private static JsonElement CreateDefaultSchema()
    {
        var defaultSchemaJson = """
            {
                "type": "object"
            }
            """;

        using var document = JsonDocument.Parse(defaultSchemaJson);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Extracts the required property names from a JsonElement schema.
    /// </summary>
    /// <param name="schema">The schema JsonElement.</param>
    /// <returns>An array of required property names.</returns>
    public static string[] GetRequiredProperties(JsonElement schema)
    {
        if (!schema.TryGetProperty("required", out var requiredProperty) || 
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
}
