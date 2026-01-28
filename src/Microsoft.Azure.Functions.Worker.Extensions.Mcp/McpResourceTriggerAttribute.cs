// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to designate a function parameter as an MCP Resource trigger.
/// </summary>
[InputConverter(typeof(ResourceInvocationContextConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpResourceTriggerAttribute(string uri, string resourceName) : TriggerBindingAttribute, IMcpBindingAttribute
{
    /// <summary>
    /// Gets or sets the URI of the MCP resource.
    /// </summary>
    public string Uri { get; } = uri;

    /// <summary>
    /// Gets or sets the name of the MCP resource.
    /// </summary>
    public string ResourceName { get; } = resourceName;

    /// <summary>
    /// Gets or sets the MIME type of the MCP resource.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Gets or sets the description of the MCP resource.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the optional size of the resource in bytes.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Gets or sets the JSON-serialized metadata for the MCP resource.
    /// </summary>
    public string? Metadata { get; init; }

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => ResourceName;
}
