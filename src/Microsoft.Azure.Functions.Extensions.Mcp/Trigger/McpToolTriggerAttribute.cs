// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

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
}
