// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Represents a single parsed MCP binding within a function.
/// Steps collect resolved data on this model; <see cref="McpBindingBuilder.Build"/>
/// serializes it all onto <see cref="JsonObject"/> in a single pass.
/// </summary>
internal sealed class McpParsedBinding(int index, string bindingType, string? identifier, JsonObject jsonObject)
{
    /// <summary>
    /// Position in <see cref="Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadata.RawBindings"/>.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// The binding type (e.g., "mcpToolTrigger", "mcpResourceTrigger", "mcpPromptTrigger", "mcpToolProperty").
    /// </summary>
    public string BindingType { get; } = bindingType;

    /// <summary>
    /// The binding-type-specific identifier: toolName, uri, promptName, or propertyName.
    /// </summary>
    public string? Identifier { get; } = identifier;

    /// <summary>
    /// The parsed JSON object for this binding. Steps should avoid writing to this directly;
    /// use the pending-change properties below and let <see cref="McpBindingBuilder.Build"/> apply them.
    /// </summary>
    public JsonObject JsonObject { get; } = jsonObject;

    // ── Pending changes: populated by steps, applied by Build() ──

    /// <summary>
    /// Serialized tool properties JSON, set by AddToolProperties step.
    /// </summary>
    public JsonNode? ToolProperties { get; set; }

    /// <summary>
    /// Serialized input schema JSON string, set during Build().
    /// </summary>
    public string? InputSchema { get; set; }

    /// <summary>
    /// Whether the worker-generated input schema flag should be set, set during Build().
    /// </summary>
    public bool UseWorkerInputSchema { get; set; }

    /// <summary>
    /// Serialized prompt arguments JSON, set by AddPromptArguments step.
    /// </summary>
    public JsonNode? PromptArguments { get; set; }

    /// <summary>
    /// Final merged metadata object, assembled by AddMetadata and AddAppUiMetadata steps.
    /// </summary>
    public JsonObject? Metadata { get; set; }

    /// <summary>
    /// Property type string, set by PatchPropertyBindings step for mcpToolProperty bindings.
    /// </summary>
    public string? PropertyType { get; set; }
}
