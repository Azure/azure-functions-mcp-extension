// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// The context for the resource invocation.
/// </summary>
public class ResourceInvocationContext
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
    /// Gets the MCP client information.
    /// </summary>
    [JsonPropertyName("clientinfo")]
    public object? ClientInfo { get; init; }

    /// <summary>
    /// Gets the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }

    /// <summary>
    /// Gets additional properties for binding data support.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonConverter(typeof(DictionaryStringObjectJsonConverter))]
    public Dictionary<string, object>? Properties { get; init; }
}
