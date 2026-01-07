// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents the result returned by an MCP tool execution.
/// Utilized when rich content type support is enabled.
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// The content returned by the tool.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
