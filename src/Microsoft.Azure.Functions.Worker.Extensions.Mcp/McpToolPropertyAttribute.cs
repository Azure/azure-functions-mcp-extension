// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class McpToolPropertyAttribute(string propertyName, string propertyType, string description, bool required = false) : InputBindingAttribute
{
    public McpToolPropertyAttribute()
    : this(string.Empty, string.Empty, string.Empty)
    {
    }

    public string PropertyName { get; set; } = propertyName;

    public string PropertyType { get; set; } = propertyType;

    public string? Description { get; set; } = description;
    
    public bool Required { get; set; } = required;
}