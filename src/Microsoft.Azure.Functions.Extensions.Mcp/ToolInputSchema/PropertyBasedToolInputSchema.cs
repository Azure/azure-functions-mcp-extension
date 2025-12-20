// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates tool request arguments using traditional tool properties.
/// </summary>
internal sealed class PropertyBasedToolInputSchema : ToolInputSchema
{
    /// <summary>
    /// Initializes a new instance of the PropertyBasedToolRequestValidator class.
    /// </summary>
    /// <param name="properties">The tool properties to use for validation.</param>
    public PropertyBasedToolInputSchema(ICollection<IMcpToolProperty> properties)
    {
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
    }

    /// <summary>
    /// Gets the list of required property names for validation.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected override IReadOnlyCollection<string> GetRequiredProperties()
    {
        return Properties
            .Where(p => p.IsRequired)
            .Select(p => p.PropertyName)
            .ToList();
    }
}
