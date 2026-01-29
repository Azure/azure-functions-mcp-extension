// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// The context for the resource invocation.
/// </summary>
public class ResourceInvocationContext(string uri)
{
    /// <summary>
    /// Gets the URI of the resource to invoke.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; } = uri;

    /// <summary>
    /// Gets or initializes the session ID associated with the current resource invocation.
    /// </summary>
    [JsonPropertyName("sessionid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets or initializes the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }
}
