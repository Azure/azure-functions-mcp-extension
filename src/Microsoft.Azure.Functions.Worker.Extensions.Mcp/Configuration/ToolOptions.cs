// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;
using System.ComponentModel;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents tool configuration options, including property definitions and their names, types, descriptions,
/// and required status.
/// </summary>
public class ToolOptions : McpBuilderOptions
{
    [Obsolete($"Use the overload with an {nameof(McpToolPropertyType)} parameter.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddProperty(string name, string type, string description, bool required = false)
    {
        Properties.Add(new ToolProperty(name, type, description, required));
    }

    /// <summary>
    /// Adds a new property definition to the tool with the specified name, type, description, and required status.
    /// </summary>
    /// <param name="name">The name of the property to add. Cannot be null or empty.</param>
    /// <param name="type">The type of the property, including information about whether it is an array.</param>
    /// <param name="description">A description of the property's purpose or usage. Cannot be null.</param>
    /// <param name="required">Indicates whether the property is required. Set to <see langword="true"/> if the property must be provided;
    /// otherwise, <see langword="false"/>.</param>
    public void AddProperty(string name, McpToolPropertyType type, string description, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        Properties.Add(new ToolProperty(name, type.TypeName, description, required, type.IsArray));
    }

    /// <summary>
    /// Gets or sets the collection of properties that define the characteristics or configuration of the tool.
    /// </summary>
    public required List<ToolProperty> Properties { get; set; } = [];

    /// <summary>
    /// MCP App configuration for this tool. Null if this tool is not an MCP App.
    /// </summary>
    public AppOptions? AppOptions { get; set; }

    /// <summary>
    /// Gets or sets an explicit JSON input schema for the tool.
    /// When set, the worker emits this schema (and <c>useWorkerInputSchema = true</c>)
    /// on the tool trigger binding, taking precedence over any host-side schema
    /// generated from <see cref="Properties"/>.
    /// </summary>
    public string? InputSchema { get; set; }

    /// <summary>
    /// Gets or sets an explicit JSON output schema.
    /// When set, this takes priority over schema generated from other sources (e.g., return type reflection).
    /// </summary>
    public string? OutputSchema { get; set; }
}
