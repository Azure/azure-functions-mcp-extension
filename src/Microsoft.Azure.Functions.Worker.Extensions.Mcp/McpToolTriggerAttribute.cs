// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;


namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class McpToolTriggerAttribute(string toolName, string? description = null) : TriggerBindingAttribute
{
    /// <summary>
    /// Gets or sets the name of the MCP tool.
    /// </summary>
    public string ToolName { get; set; } = toolName;

    /// <summary>
    /// Gets or sets the description of the MCP tool.
    /// </summary>
    public string? Description { get; set; } = description;
}
