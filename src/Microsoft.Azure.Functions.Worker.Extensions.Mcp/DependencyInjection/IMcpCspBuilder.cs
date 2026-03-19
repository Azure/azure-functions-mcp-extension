// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Configures the Content Security Policy for an MCP App view.
/// </summary>
public interface IMcpCspBuilder
{
    /// <summary>Adds an origin for network requests (fetch/XHR/WebSocket). Maps to CSP connect-src.</summary>
    IMcpCspBuilder ConnectTo(string origin);

    /// <summary>Adds an origin for static resources (scripts, images, styles, fonts, media). Maps to CSP resource directives.</summary>
    IMcpCspBuilder LoadResourcesFrom(string origin);

    /// <summary>Adds an origin for nested iframes. Maps to CSP frame-src.</summary>
    IMcpCspBuilder AllowFrame(string origin);

    /// <summary>Adds an allowed base URI for the document. Maps to CSP base-uri.</summary>
    IMcpCspBuilder AllowBaseUri(string origin);
}
