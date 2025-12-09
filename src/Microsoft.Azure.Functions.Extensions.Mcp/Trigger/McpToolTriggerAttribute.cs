// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Trigger;
using Microsoft.Azure.WebJobs.Description;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[AttributeUsage(AttributeTargets.Parameter)]
#pragma warning disable CS0618 // Type or member is obsolete
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class McpToolTriggerAttribute(string toolName, string? description) : Attribute
{
    public string ToolName { get; } = toolName;

    public string? Description { get; set; } = description;

    public string? ToolProperties { get; set; }

    public bool UseResultSchema { get; set; } = false;

    public bool UseWorkerInputSchema { get; set; } = false;

    /// <summary>
    /// Gets or sets the input schema as a JSON element. 
    /// When not set, the schema will be generated from tool properties.
    /// </summary>
    public JsonElement? InputSchema { get; set; }
}
