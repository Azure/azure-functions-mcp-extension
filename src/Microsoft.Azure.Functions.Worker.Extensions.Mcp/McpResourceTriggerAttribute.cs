// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[InputConverter(typeof(ResourceInvocationContextConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
public sealed class McpResourceTriggerAttribute(string uri, string resourceName) : TriggerBindingAttribute, IMcpBindingAttribute
{
    /// <summary>
    /// Gets or sets the URI of the MCP resource.
    /// </summary>
    public string Uri { get; set; } = uri;

    /// <summary>
    /// Gets or sets the name of the MCP resource.
    /// </summary>
    public string ResourceName { get; set; } = resourceName;

    /// <summary>
    /// Gets or sets the MIME type of the MCP resource.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the description of the MCP resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the size of the MCP resource.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized metadata for the MCP resource.
    /// This is automatically populated from <see cref="McpResourceMetadataAttribute"/> attributes.
    /// </summary>
    public string? Metadata { get; set; }

    /// <inheritdoc />
    string IMcpBindingAttribute.BindingName => ResourceName;
}
