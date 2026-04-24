// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// A strongly-typed, validated MCP tool input JSON schema.
/// An <see cref="McpInputSchema"/> instance can only exist when the schema
/// conforms to the MCP tool input-schema shape expected by the host — once constructed,
/// consumers can use <see cref="McpToolSchema.Json"/> without re-validating.
/// </summary>
public sealed class McpInputSchema : McpToolSchema
{
    /// <summary>
    /// Initializes a new instance from a JSON schema string.
    /// </summary>
    /// <param name="json">A valid JSON schema string with root <c>"type": "object"</c>.</param>
    /// <exception cref="ArgumentException">Thrown when the schema is null/whitespace or violates MCP shape rules.</exception>
    /// <exception cref="JsonException">Thrown when the schema is not valid JSON.</exception>
    public McpInputSchema(string json) : base(json, nameof(json)) { }

    /// <summary>
    /// Initializes a new instance from a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    public McpInputSchema(JsonNode schemaNode) : base(schemaNode, nameof(schemaNode)) { }

    private protected override string SchemaKind => "Input";
}
