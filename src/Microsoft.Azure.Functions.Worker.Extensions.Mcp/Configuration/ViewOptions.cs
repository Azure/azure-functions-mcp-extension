// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Configuration options for a single view within an MCP App.
/// </summary>
public class ViewOptions
{
    /// <summary>
    /// The content source for this view. Guaranteed non-null when configured
    /// through the builder API. May be null if options are manipulated directly.
    /// </summary>
    public McpViewSource? Source { get; set; }

    /// <summary>
    /// The display title for this view. Null if not set.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Whether the host should render a border around the view. Defaults to false.
    /// </summary>
    public bool Border { get; set; }

    /// <summary>
    /// Domain hint for the view, used by the host to scope cookies and storage.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Content Security Policy configuration for this view.
    /// </summary>
    public CspOptions? Csp { get; set; }

    /// <summary>
    /// Permissions granted to this view. Defaults to None.
    /// </summary>
    public McpAppPermissions Permissions { get; set; } = McpAppPermissions.None;
}
