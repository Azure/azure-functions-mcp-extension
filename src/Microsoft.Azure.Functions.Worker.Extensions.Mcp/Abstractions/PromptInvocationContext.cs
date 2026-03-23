// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// The context for the prompt invocation.
/// </summary>
public class PromptInvocationContext
{
    /// <summary>
    /// Gets the name of the prompt.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the arguments provided for the prompt.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, string>? Arguments { get; init; }

    /// <summary>
    /// Gets the session ID associated with the current prompt invocation.
    /// </summary>
    [JsonPropertyName("sessionid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the transport information.
    /// </summary>
    [JsonPropertyName("transport")]
    public Transport? Transport { get; init; }
}
