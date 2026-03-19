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
}
