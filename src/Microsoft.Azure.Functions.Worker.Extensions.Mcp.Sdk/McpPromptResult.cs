// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Envelope used by the worker SDK middleware to carry a prompt function's
/// return value across the worker/host boundary. Mirrors <see cref="McpToolResult"/>
/// so the host can deterministically deserialize the inner payload based on
/// <see cref="Type"/>, without inspecting the payload shape.
/// </summary>
internal sealed class McpPromptResult
{
    /// <summary>
    /// The serialized inner payload (e.g. a <c>GetPromptResult</c> or list of <c>PromptMessage</c>).
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Discriminator describing the shape of <see cref="Content"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
