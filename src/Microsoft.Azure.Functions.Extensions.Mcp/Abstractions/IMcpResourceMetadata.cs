// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents metadata associated with an MCP resource.
/// </summary>
public interface IMcpResourceMetadata
{
    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    object? Value { get; }
}