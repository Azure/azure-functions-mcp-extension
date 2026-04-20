// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Provides a base implementation for MCP builder options classes that support metadata configuration.
/// </summary>
public abstract class McpBuilderOptions
{
    /// <summary>
    /// Gets the metadata associated with the options.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets an explicit JSON input schema.
    /// When set, this takes priority over schema generated from other sources (e.g., properties or reflection).
    /// </summary>
    public string? InputSchema { get; set; }
}
