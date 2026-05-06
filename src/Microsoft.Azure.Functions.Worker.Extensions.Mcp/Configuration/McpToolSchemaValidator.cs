// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Validates that a JSON schema conforms to the MCP tool schema shape expected by
/// the host. Shared by both input and output schema wrappers.
/// </summary>
internal static class McpToolSchemaValidator
{
    /// <summary>
    /// Parses <paramref name="jsonSchema"/> into a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="jsonSchema">JSON schema string.</param>
    /// <param name="kind">Schema kind used for exception messages.</param>
    /// <param name="paramName">Parameter name used in thrown exceptions.</param>
    /// <returns>The parsed <see cref="JsonNode"/> on success.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is null/whitespace or resolves to null.</exception>
    /// <exception cref="JsonException">Thrown when the schema is not valid JSON.</exception>
    public static JsonNode Parse(string jsonSchema, SchemaKind kind, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonSchema, paramName);

        return JsonNode.Parse(jsonSchema)
            ?? throw new ArgumentException($"{kind} schema must be a JSON object.", paramName);
    }

    /// <summary>
    /// Validates a parsed schema node.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the schema does not match MCP requirements.</exception>
    public static void Validate(JsonNode schemaNode, SchemaKind kind, string paramName)
    {
        ArgumentNullException.ThrowIfNull(schemaNode, paramName);

        if (schemaNode is not JsonObject root)
        {
            throw new ArgumentException($"{kind} schema must be a JSON object.", paramName);
        }

        if (!root.TryGetPropertyValue("type", out var typeNode)
            || typeNode is not JsonValue typeValue
            || !typeValue.TryGetValue<string>(out var typeStr)
            || typeStr != "object")
        {
            throw new ArgumentException($"{kind} schema must have a root \"type\" property with value \"object\".", paramName);
        }

        if (root.TryGetPropertyValue("properties", out var propsNode)
            && propsNode is not JsonObject)
        {
            throw new ArgumentException($"{kind} schema \"properties\" must be a JSON object when present.", paramName);
        }

        if (root.TryGetPropertyValue("required", out var requiredNode)
            && requiredNode is not JsonArray)
        {
            throw new ArgumentException($"{kind} schema \"required\" must be a JSON array when present.", paramName);
        }
    }
}
