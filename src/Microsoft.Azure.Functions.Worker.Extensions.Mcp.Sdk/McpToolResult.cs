// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
    [UseJsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned.
    /// </summary>
    [UseJsonPropertyName("type")]
    public required string Type { get; set; }
}
