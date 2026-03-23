// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to designate a function parameter as an MCP prompt argument input binding.
/// </summary>
[InputConverter(typeof(PromptArgumentConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpPromptArgumentAttribute(string argumentName, string? description = null, bool isRequired = false) : InputBindingAttribute, IMcpBindingAttribute
{
    public McpPromptArgumentAttribute()
        : this(string.Empty)
    {
    }

    /// <summary>
    /// Gets or sets the name of the prompt argument.
    /// </summary>
    public string ArgumentName { get; set; } = argumentName;

    /// <summary>
    /// Gets or sets the description of the prompt argument.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets or sets a value indicating whether the prompt argument is required.
    /// </summary>
    public bool IsRequired { get; set; } = isRequired;

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => ArgumentName;
}
