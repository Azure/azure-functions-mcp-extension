// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Configuration options for an MCP App tool, including views, static assets, and visibility.
/// </summary>
public class AppOptions
{
    /// <summary>
    /// Views registered for this MCP App. Keyed by view name.
    /// The default unnamed view uses <see cref="string.Empty"/> as its key.
    /// </summary>
    public Dictionary<string, ViewOptions> Views { get; } = new();

    /// <summary>
    /// Directory from which static assets are served. Null if not configured.
    /// </summary>
    public string? StaticAssetsDirectory { get; set; }

    /// <summary>
    /// Static asset serving options. Null if static assets are not configured.
    /// </summary>
    public StaticAssetOptions? StaticAssets { get; set; }

    /// <summary>
    /// Visibility of this tool. Defaults to Model | App.
    /// </summary>
    public McpVisibility Visibility { get; set; } = McpVisibility.Model | McpVisibility.App;
}
