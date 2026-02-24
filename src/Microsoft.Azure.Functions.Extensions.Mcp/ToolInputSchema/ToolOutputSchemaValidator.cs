// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates that a <see cref="CallToolResult"/> contains structured content
/// conforming to the declared output schema.
/// When an output schema is declared, the MCP specification requires that
/// servers MUST provide structured results that conform to the schema.
/// </summary>
internal static class ToolOutputSchemaValidator
{
    /// <summary>
    /// Validates the structured content of a <see cref="CallToolResult"/> against the output schema.
    /// </summary>
    /// <param name="outputSchema">The declared output schema as a <see cref="JsonElement"/>.</param>
    /// <param name="callToolResult">The tool result to validate.</param>
    /// <exception cref="McpProtocolException">
    /// Thrown when the result is missing structured content, is missing required properties,
    /// or has property type mismatches compared to the declared output schema.
    /// </exception>
    public static void Validate(JsonElement outputSchema, CallToolResult callToolResult)
    {
        ArgumentNullException.ThrowIfNull(callToolResult);

        if (callToolResult.StructuredContent is not JsonObject structuredContent)
        {
            throw new McpProtocolException(
                "Output schema is declared but the tool result does not contain structured content. " +
                "When an output schema is provided, the tool must return structured content conforming to the schema.",
                McpErrorCode.InvalidParams);
        }

        ValidateRequiredProperties(outputSchema, structuredContent);
        ValidatePropertyTypes(outputSchema, structuredContent);
    }

    private static void ValidateRequiredProperties(JsonElement outputSchema, JsonObject structuredContent)
    {
        if (!outputSchema.TryGetProperty("required", out var requiredProperty)
            || requiredProperty.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var missing = new List<string>();

        foreach (var item in requiredProperty.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not string propertyName)
            {
                continue;
            }

            if (!structuredContent.ContainsKey(propertyName)
                || IsNullNode(structuredContent[propertyName]))
            {
                missing.Add(propertyName);
            }
        }

        if (missing.Count > 0)
        {
            var names = string.Join(", ", missing);
            throw new McpProtocolException(
                $"Structured content is missing required properties declared in the output schema. Please provide: {names}",
                McpErrorCode.InvalidParams);
        }
    }

    /// <summary>
    /// Validates that the types of properties in the structured content match
    /// the types declared in the output schema's "properties" object.
    /// </summary>
    private static void ValidatePropertyTypes(JsonElement outputSchema, JsonObject structuredContent)
    {
        if (!outputSchema.TryGetProperty("properties", out var schemaProperties)
            || schemaProperties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var mismatches = new List<string>();

        foreach (var kvp in structuredContent)
        {
            var propertyName = kvp.Key;
            var propertyValue = kvp.Value;

            if (!schemaProperties.TryGetProperty(propertyName, out var propertySchema))
            {
                // Property not declared in schema — skip (additionalProperties handling is out of scope).
                continue;
            }

            if (!propertySchema.TryGetProperty("type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String)
            {
                // No type declaration or non-string type (e.g., array of types) — skip.
                continue;
            }

            var expectedType = typeElement.GetString();
            if (expectedType is null)
            {
                continue;
            }

            if (!IsValueKindCompatible(propertyValue, expectedType))
            {
                var actualType = GetJsonNodeTypeName(propertyValue);
                mismatches.Add($"'{propertyName}' (expected {expectedType}, got {actualType})");
            }
        }

        if (mismatches.Count > 0)
        {
            var details = string.Join(", ", mismatches);
            throw new McpProtocolException(
                $"Structured content has property type mismatches with the output schema: {details}",
                McpErrorCode.InvalidParams);
        }
    }

    /// <summary>
    /// Checks if a <see cref="JsonNode"/> value is compatible with a JSON Schema type string.
    /// </summary>
    private static bool IsValueKindCompatible(JsonNode? node, string expectedType)
    {
        if (node is null)
        {
            // Null is only valid for "null" type; otherwise it's a mismatch.
            return expectedType == "null";
        }

        return expectedType switch
        {
            "string" => node is JsonValue jv && jv.GetValueKind() == JsonValueKind.String,
            "number" => node is JsonValue jvn && jvn.GetValueKind() == JsonValueKind.Number,
            "integer" => node is JsonValue jvi && jvi.GetValueKind() == JsonValueKind.Number,
            "boolean" => node is JsonValue jvb && (jvb.GetValueKind() == JsonValueKind.True || jvb.GetValueKind() == JsonValueKind.False),
            "object" => node is JsonObject,
            "array" => node is JsonArray,
            "null" => false, // non-null node for "null" type
            _ => true // Unknown type — don't reject
        };
    }

    /// <summary>
    /// Gets a human-readable type name for a <see cref="JsonNode"/>.
    /// </summary>
    private static string GetJsonNodeTypeName(JsonNode? node)
    {
        return node switch
        {
            null => "null",
            JsonObject => "object",
            JsonArray => "array",
            JsonValue jv => jv.GetValueKind() switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Null => "null",
                _ => "unknown"
            },
            _ => "unknown"
        };
    }

    private static bool IsNullNode(JsonNode? node)
    {
        return node is null;
    }
}
