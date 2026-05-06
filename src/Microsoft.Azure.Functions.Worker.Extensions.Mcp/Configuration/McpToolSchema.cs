// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Base class for strongly-typed, validated MCP tool JSON schemas (input or output).
/// A derived instance can only exist when the schema conforms to the MCP tool schema
/// shape expected by the host — once constructed, consumers can use <see cref="Json"/>
/// without re-validating.
/// </summary>
public abstract class McpToolSchema
{
    private protected McpToolSchema(string json, string paramName, SchemaKind kind)
    {
        var node = McpToolSchemaValidator.Parse(json, kind, paramName);
        McpToolSchemaValidator.Validate(node, kind, paramName);
        Json = node.ToJsonString();
    }

    private protected McpToolSchema(JsonNode schemaNode, string paramName, SchemaKind kind)
    {
        McpToolSchemaValidator.Validate(schemaNode, kind, paramName);
        Json = schemaNode.ToJsonString();
    }

    /// <summary>
    /// The canonical JSON representation of the validated schema.
    /// </summary>
    public string Json { get; }

    /// <summary>
    /// Returns the canonical JSON representation of the schema.
    /// </summary>
    public override string ToString() => Json;
}
