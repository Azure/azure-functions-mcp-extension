// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[InputConverter(typeof(ToolInvocationContextConverter))]
[InputConverter(typeof(ToolInvocationPocoConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpToolTriggerAttribute(string toolName, string? description = null) : TriggerBindingAttribute, IMcpBindingAttribute
{
    /// <summary>
    /// Gets or sets the name of the MCP tool.
    /// </summary>
    public string ToolName { get; set; } = toolName;

    /// <summary>
    /// Gets or sets the description of the MCP tool.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets or sets a human-readable title for the tool that can be displayed to users.
    /// Unlike <see cref="ToolName"/>, the title can include spaces and natural-language phrasing.
    /// Advertised on <c>tools/list</c> as both the tool title and <c>annotations.title</c>.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether this tool does not modify its environment.
    /// Surfaced on <c>tools/list</c> as <c>annotations.readOnlyHint</c>. Only advertised when set.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the tool might perform destructive updates to its environment.
    /// Surfaced on <c>tools/list</c> as <c>annotations.destructiveHint</c>. Only advertised when set.
    /// This hint is most relevant when the tool modifies its environment (<see cref="ReadOnly"/> is <see langword="false"/>).
    /// </summary>
    public bool Destructive { get; set; }

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => ToolName;
}
