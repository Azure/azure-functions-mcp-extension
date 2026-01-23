// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents tool configuration options, including property definitions and their names, types, descriptions,
/// and required status.
/// </summary>
public class ToolOptions
{
    private readonly JsonObject _metadata = [];

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
    /// Adds metadata for the tool using a key-value pair.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value (can be a string, number, object, or array).</param>
    public void AddMetadata(string key, JsonNode? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        
        _metadata[key] = value;
    }

    /// <summary>
    /// Gets or sets the collection of properties that define the characteristics or configuration of the tool.
    /// </summary>
    public required List<ToolProperty> Properties { get; set; } = [];

    /// <summary>
    /// Gets the serialized tool metadata JSON (object) if configured.
    /// </summary>
    public string? MetadataJson => _metadata.Count > 0 ? _metadata.ToJsonString() : null;
}
