// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Attribute to designate a function parameter as an MCP Prompt trigger.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
#pragma warning disable CS0618 // Type or member is obsolete
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class McpPromptTriggerAttribute(string promptName) : Attribute
{
    /// <summary>
    /// Gets the name of the prompt.
    /// </summary>
    public string PromptName { get; } = promptName;

    /// <summary>
    /// Gets or sets an optional human-readable title for display purposes.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the prompt.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the prompt arguments schema.
    /// </summary>
    public string? PromptArguments { get; set; }

    /// <summary>
    /// Gets or sets the JSON metadata for the prompt.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the prompt icons.
    /// </summary>
    public string? Icons { get; set; }
}
