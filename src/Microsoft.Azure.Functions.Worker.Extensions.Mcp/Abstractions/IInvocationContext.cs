// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public interface IInvocationContext
{
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