// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents the result returned by an MCP tool execution.
/// Utilized when rich content type support is enabled.
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// The content returned by the tool.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned.
    /// </summary>
    public string? Type { get; set; }
}
