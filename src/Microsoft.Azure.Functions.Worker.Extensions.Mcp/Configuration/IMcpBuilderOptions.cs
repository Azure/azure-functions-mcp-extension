// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Defines a contract for MCP builder options classes that support metadata configuration.
/// </summary>
public interface IMcpBuilderOptions
{
    /// <summary>
    /// Gets the metadata dictionary.
    /// </summary>
    Dictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Adds a metadata entry with the specified key and value.
    /// </summary>
    /// <param name="key">The key for the metadata entry.</param>
    /// <param name="value">The value for the metadata entry.</param>
    void AddMetadata(string key, object? value);
}
