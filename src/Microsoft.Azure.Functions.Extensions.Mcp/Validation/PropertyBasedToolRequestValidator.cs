// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates tool request arguments using traditional tool properties.
/// </summary>
internal sealed class PropertyBasedToolRequestValidator : ToolRequestValidator
{
    private readonly ICollection<IMcpToolProperty> _properties;

    /// <summary>
    /// Initializes a new instance of the PropertyBasedToolRequestValidator class.
    /// </summary>
    /// <param name="properties">The tool properties to use for validation.</param>
    public PropertyBasedToolRequestValidator(ICollection<IMcpToolProperty> properties)
    {
        _properties = properties ?? throw new ArgumentNullException(nameof(properties));
    }

    /// <summary>
    /// Gets the list of required property names for validation.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected override IReadOnlyCollection<string> GetRequiredProperties()
    {
        return _properties
            .Where(p => p.IsRequired)
            .Select(p => p.PropertyName)
            .ToList();
    }
}
