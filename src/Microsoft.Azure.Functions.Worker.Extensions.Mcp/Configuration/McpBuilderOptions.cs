// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Provides a base implementation for MCP builder options classes that support metadata configuration.
/// </summary>
public abstract class McpBuilderOptions : IMcpBuilderOptions
{
    /// <summary>
    /// Gets or sets the metadata associated with the options.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];

    /// <inheritdoc />
    public void AddMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        Metadata[key] = value;
    }
}
