// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Content Security Policy configuration for an MCP App view,
/// following the MCP Apps spec (SEP-1865).
/// </summary>
public class CspOptions
{
    /// <summary>Origins for network requests (fetch/XHR/WebSocket). Maps to CSP connect-src directive.</summary>
    public List<string> ConnectDomains { get; } = new();

    /// <summary>Origins for static resources (images, scripts, stylesheets, fonts, media). Maps to CSP img-src, script-src, style-src, font-src, media-src directives.</summary>
    public List<string> ResourceDomains { get; } = new();

    /// <summary>Origins for nested iframes. Maps to CSP frame-src directive.</summary>
    public List<string> FrameDomains { get; } = new();

    /// <summary>Allowed base URIs for the document. Maps to CSP base-uri directive.</summary>
    public List<string> BaseUriDomains { get; } = new();
}
