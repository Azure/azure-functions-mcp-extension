// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[InputConverter(typeof(ToolInvocationArrayConverter))]
[InputConverter(typeof(ToolInvocationArgumentTypeConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpToolPropertyAttribute(string propertyName, string propertyType, string description, bool required = false) : InputBindingAttribute, IMcpBindingAttribute
{
    public McpToolPropertyAttribute()
    : this(string.Empty, string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// Gets or sets the name of the MCP tool property.
    /// </summary>
    public string PropertyName { get; set; } = propertyName;

    /// <summary>
    /// Gets or sets the type of the MCP tool property.
    /// </summary>
    public string PropertyType { get; set; } = propertyType;

    /// <summary>
    /// Gets or sets the description of the MCP tool property.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets or sets a value indicating whether the MCP tool property is required.
    /// </summary>
    public bool Required { get; set; } = required;

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => PropertyName;
}
