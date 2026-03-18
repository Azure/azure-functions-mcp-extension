// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Content Security Policy options for an MCP App view.
/// Maps to <c>_meta.ui.csp</c> in the MCP protocol.
/// </summary>
public class AppCspOptions
{
    /// <summary>
    /// External origins the view is allowed to connect to (fetch, XHR, WebSocket).
    /// Maps to <c>_meta.ui.csp.connectDomains</c>.
    /// </summary>
    public List<string>? ConnectDomains { get; set; }

    /// <summary>
    /// External origins the view is allowed to load resources from (scripts, styles, images).
    /// Maps to <c>_meta.ui.csp.resourceDomains</c>.
    /// </summary>
    public List<string>? ResourceDomains { get; set; }
}
