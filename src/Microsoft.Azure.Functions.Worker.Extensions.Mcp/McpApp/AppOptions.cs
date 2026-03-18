// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Configuration for an MCP App associated with a tool.
/// Captures the data needed to generate <c>_meta.ui</c> metadata
/// and a synthetic resource function for serving the app's HTML.
/// </summary>
public class AppOptions
{
    /// <summary>
    /// The <c>ui://</c> resource URI for this app's view.
    /// Example: <c>ui://get_weather/index.html</c>.
    /// </summary>
    public required string ResourceUri { get; set; }

    /// <summary>
    /// Controls where the tool is visible (model, app, or both).
    /// Defaults to <see cref="McpVisibility.ModelAndApp"/>.
    /// </summary>
    public McpVisibility Visibility { get; set; } = McpVisibility.ModelAndApp;

    /// <summary>
    /// Relative path to the HTML file to serve for this app's view.
    /// Resolved relative to the function app's root directory.
    /// </summary>
    public string? ViewFilePath { get; set; }

    /// <summary>
    /// Optional human-readable title displayed by the host alongside the view.
    /// </summary>
    public string? ViewTitle { get; set; }

    /// <summary>
    /// When set, requests the host to render (or not render) a border around the view.
    /// </summary>
    public bool? PrefersBorder { get; set; }

    /// <summary>
    /// A dedicated sandbox origin for this view, used for OAuth callbacks and CORS.
    /// Maps to <c>_meta.ui.domain</c>.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Content Security Policy configuration for the view.
    /// </summary>
    public AppCspOptions? Csp { get; set; }

    /// <summary>
    /// Browser permissions requested by the view.
    /// </summary>
    public McpAppPermissions Permissions { get; set; } = McpAppPermissions.None;

    /// <summary>
    /// Directory containing static assets (JS, CSS, images) to serve via HTTP.
    /// </summary>
    public string? StaticAssetsDirectory { get; set; }
}
