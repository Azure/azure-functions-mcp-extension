// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ToolInvocationContext
{
    /// <summary>
    /// Gets or sets the name of the tool to invoke.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets optional arguments to pass to the tool when invoking it on the server.
    /// </summary>
    /// <remarks>
    /// This dictionary contains the parameter values to be passed to the tool.
    /// Each key-value pair represents a parameter name and its corresponding argument value.
    /// </remarks>
    [JsonPropertyName("arguments")]
    public IReadOnlyDictionary<string, JsonElement>? Arguments { get; init; }


    /// <summary>
    /// Gets the session ID associated with the current tool invocation.
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
    public  Transport? Transport { get; init; }
}
