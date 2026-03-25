// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Configuration options for static asset serving in an MCP App.
/// </summary>
public class StaticAssetOptions
{
    /// <summary>
    /// Whether to serve .map (source map) files. Defaults to false.
    /// Source maps can leak internal paths and implementation details.
    /// </summary>
    public bool IncludeSourceMaps { get; set; }
}
