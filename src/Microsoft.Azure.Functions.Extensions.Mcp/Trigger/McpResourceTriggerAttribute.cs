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
    public string Uri { get; set; } = uri;

    public string ResourceName { get; set; } = resourceName;

    public string? MimeType { get; set; }

    public string? Description { get; set; }

    public long? Size { get; set; }

    public string? Metadata { get; set; }
}