// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents the context for an MCP resource invocation.
/// </summary>
public sealed class ResourceInvocationContext : IInvocationContext
{
    /// <summary>
    /// Gets or sets the URI of the resource to invoke.
    /// </summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>
    /// Gets the session ID associated with the current resource invocation.
    /// </summary>
    [JsonPropertyName("sessionid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }
}
