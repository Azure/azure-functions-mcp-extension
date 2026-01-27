// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ResourceInvocationContext(string uri)
{
    /// <summary>
    /// Gets or sets the URI of the resource to invoke.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; } = uri;

    /// <summary>
    /// Gets the session ID associated with the current resource invocation.
    /// </summary>
    [JsonPropertyName("sessionid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the MCP client information.
    /// </summary>
    [JsonPropertyName("clientinfo")]
    public Implementation? ClientInfo { get; init; }

    /// <summary>
    /// Gets the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }
}
