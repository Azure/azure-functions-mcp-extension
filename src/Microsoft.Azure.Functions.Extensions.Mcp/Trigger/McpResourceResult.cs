// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents the result returned by an MCP resource read operation.
/// </summary>
public sealed class McpResourceResult
{
    /// <summary>
    /// The content returned by the resource read operation.
    /// This should be a JSON-serialized string representation of either TextResourceContents or BlobResourceContents.
    /// </summary>
    public required string Content { get; set; }
}
