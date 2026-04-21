// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Validates that a JSON input schema conforms to the MCP tool input-schema shape
/// expected by the host (mirrors <c>McpInputSchemaJsonUtilities.IsValidMcpToolSchema</c>).
/// </summary>
internal static class InputSchemaValidator
{
    /// <summary>
    /// Parses <paramref name="jsonSchema"/> and validates its shape.
    /// </summary>
    /// <param name="jsonSchema">JSON schema string.</param>
    /// <param name="paramName">Parameter name used in thrown exceptions.</param>
    /// <returns>The parsed <see cref="JsonNode"/> on success.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is null/empty, not an object, or violates MCP shape rules.</exception>
    /// <exception cref="JsonException">Thrown when the schema is not valid JSON.</exception>
    public static JsonNode ValidateAndParse(string jsonSchema, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonSchema, paramName);

        var node = JsonNode.Parse(jsonSchema)
            ?? throw new ArgumentException("Input schema must be a JSON object.", paramName);

        Validate(node, paramName);
        return node;
    }

    /// <summary>
    /// Validates a parsed schema node.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the schema does not match MCP requirements.</exception>
    public static void Validate(JsonNode schemaNode, string paramName)
    {
        ArgumentNullException.ThrowIfNull(schemaNode, paramName);

        if (schemaNode is not JsonObject root)
        {
            throw new ArgumentException("Input schema must be a JSON object.", paramName);
        }

        if (!root.TryGetPropertyValue("type", out var typeNode)
            || typeNode is not JsonValue typeValue
            || !typeValue.TryGetValue<string>(out var typeStr)
            || typeStr != "object")
        {
            throw new ArgumentException("Input schema must have a root \"type\" property with value \"object\".", paramName);
        }

        if (root.TryGetPropertyValue("properties", out var propsNode)
            && propsNode is not JsonObject)
        {
            throw new ArgumentException("Input schema \"properties\" must be a JSON object when present.", paramName);
        }

        if (root.TryGetPropertyValue("required", out var requiredNode)
            && requiredNode is not JsonArray)
        {
            throw new ArgumentException("Input schema \"required\" must be a JSON array when present.", paramName);
        }
    }
}
