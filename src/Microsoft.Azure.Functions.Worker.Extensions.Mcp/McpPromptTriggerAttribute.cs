// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to designate a function parameter as an MCP Prompt trigger.
/// </summary>
[InputConverter(typeof(PromptInvocationContextConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpPromptTriggerAttribute(string promptName) : TriggerBindingAttribute, IMcpBindingAttribute
{
    /// <summary>
    /// Gets the name of the MCP prompt.
    /// </summary>
    public string PromptName { get; } = !string.IsNullOrEmpty(promptName)
        ? promptName
        : throw new ArgumentException("Prompt name cannot be null or empty.", nameof(promptName));

    /// <summary>
    /// Gets an optional human-readable title for display purposes.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the description of the MCP prompt.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the JSON representation of the prompt arguments schema.
    /// </summary>
    public string? PromptArguments { get; init; }

    /// <summary>
    /// Gets the JSON-serialized metadata for the MCP prompt.
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Gets the JSON representation of the prompt icons.
    /// </summary>
    public string? Icons { get; init; }

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => PromptName;
}
