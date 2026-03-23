// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents the result returned by an MCP prompt execution.
/// Utilized when rich content type support is enabled via the SDK extension.
/// </summary>
public class McpPromptResult
{
    /// <summary>
    /// The serialized content returned by the prompt.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned (e.g., "get_prompt_result", "prompt_messages", "text").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
