// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents the result returned by an MCP prompt execution.
/// Utilized when rich content type support is enabled via the SDK extension.
/// </summary>
public sealed class McpPromptResult
{
    /// <summary>
    /// The serialized content returned by the prompt.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// The type of content being returned.
    /// </summary>
    public required string Type { get; init; }
}
