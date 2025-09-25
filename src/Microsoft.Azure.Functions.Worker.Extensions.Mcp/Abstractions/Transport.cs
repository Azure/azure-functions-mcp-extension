// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents a base class for different types of MCP transports.
/// </summary>
/// <remarks>This class is intended to be inherited by specific transport implementations. It provides a common
/// abstraction for transport-related functionality and metadata.</remarks>
[JsonConverter(typeof(TransportJsonConverter))]
public abstract class Transport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
