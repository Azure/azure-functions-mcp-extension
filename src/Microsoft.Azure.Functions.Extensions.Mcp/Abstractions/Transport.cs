// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class Transport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }


    /// <summary>
    /// Gets the transport name.
    /// </summary>
    [JsonPropertyName("sesssionId")]
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets the properties associated with the transport.
    /// </summary>
    [JsonPropertyName("properties")]
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
