// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// A strongly-typed, validated MCP tool input JSON schema.
/// An <see cref="McpInputSchema"/> instance can only exist when the schema
/// conforms to the MCP tool input-schema shape expected by the host — once constructed,
/// consumers can use <see cref="Json"/> without re-validating.
/// </summary>
public sealed class McpInputSchema
{
    /// <summary>
    /// Initializes a new instance from a JSON schema string.
    /// </summary>
    /// <param name="json">A valid JSON schema string with root <c>"type": "object"</c>.</param>
    /// <exception cref="ArgumentException">Thrown when the schema is null/empty or violates MCP shape rules.</exception>
    /// <exception cref="JsonException">Thrown when the schema is not valid JSON.</exception>
    public McpInputSchema(string json)
    {
        var node = InputSchemaValidator.ValidateAndParse(json, nameof(json));
        Json = node.ToJsonString();
    }

    /// <summary>
    /// Initializes a new instance from a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    public McpInputSchema(JsonNode schemaNode)
    {
        ArgumentNullException.ThrowIfNull(schemaNode);
        InputSchemaValidator.Validate(schemaNode, nameof(schemaNode));
        Json = schemaNode.ToJsonString();
    }

    /// <summary>
    /// The canonical JSON representation of the schema.
    /// </summary>
    public string Json { get; }
}
