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
    /// The content returned by the tool. Expected to be in the MCP content schema format.
    /// OR, do we want this to be the full rpc json? I don't think the worker has all the information to build the full rpc json.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned.
    /// </summary>
    public McpContentToolType Type { get; set; }
}

/// <summary>
/// Do we want to use MCP content types? But need to add raw support as well.
/// </summary>
public enum McpContentToolType
{
    Text,
    Audio,
    Image,
    ResourceLink,
    Raw
}

