// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// The context for the tool call.
/// </summary>
public class ToolInvocationContext : IInvocationContext
{
    /// <summary>
    /// Tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Optional arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonConverter(typeof(DictionaryStringObjectJsonConverter))]
    public Dictionary<string, object>? Arguments { get; init; }

    /// <summary>
    /// Gets the session ID associated with the current tool invocation.
    /// </summary>
    [JsonPropertyName("sessionid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }
}
