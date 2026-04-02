// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Represents a single parsed MCP binding within a function.
/// </summary>
internal sealed class McpParsedBinding(int index, string bindingType, string? identifier, JsonObject jsonObject)
{
    /// <summary>
    /// Position in <see cref="Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadata.RawBindings"/>.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// The binding type (e.g., "mcpToolTrigger", "mcpResourceTrigger", "mcpToolProperty").
    /// </summary>
    public string BindingType { get; } = bindingType;

    /// <summary>
    /// The binding-type-specific identifier: toolName, uri, or propertyName.
    /// </summary>
    public string? Identifier { get; } = identifier;

    /// <summary>
    /// The parsed JSON object for this binding.
    /// </summary>
    public JsonObject JsonObject { get; } = jsonObject;
}
