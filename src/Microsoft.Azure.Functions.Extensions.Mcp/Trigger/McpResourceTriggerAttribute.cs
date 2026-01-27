// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Attribute to designate a function parameter as an MCP Resource trigger.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
#pragma warning disable CS0618 // Type or member is obsolete
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class McpResourceTriggerAttribute(string uri, string resourceName) : Attribute
{
    /// <summary>
    /// Gets or sets the unique identifier for the resource.
    /// </summary>
    public string Uri { get; } = uri;

    /// <summary>
    /// Gets or sets the name of the resource.
    /// </summary>
    public string ResourceName { get; } = resourceName;

    /// <summary>
    /// Gets or sets an optional human-readable title for display purposes.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets an optional description of the resource.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the optional MIME type of the resource.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Gets or sets the optional size of the resource in bytes.
    /// </summary>
    public long? Size { get; init; }
}
