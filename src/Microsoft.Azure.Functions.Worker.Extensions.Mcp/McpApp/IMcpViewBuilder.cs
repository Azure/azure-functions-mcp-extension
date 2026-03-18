// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Configures the view (HTML resource) for an MCP App.
/// </summary>
public interface IMcpViewBuilder
{
    /// <summary>
    /// Sets the HTML file path for this view.
    /// Path is relative to the function app's root directory.
    /// </summary>
    IMcpViewBuilder FromFile(string filePath);

    /// <summary>
    /// Sets a human-readable title displayed by the host alongside the view.
    /// </summary>
    IMcpViewBuilder WithTitle(string title);

    /// <summary>
    /// Requests the host render a visible border and background around the view.
    /// </summary>
    IMcpViewBuilder WithBorder();

    /// <summary>
    /// Requests the host render no border or background around the view.
    /// </summary>
    IMcpViewBuilder WithoutBorder();

    /// <summary>
    /// Sets a dedicated sandbox origin for this view.
    /// </summary>
    IMcpViewBuilder WithDomain(string domain);

    /// <summary>
    /// Configures the Content Security Policy for this view.
    /// </summary>
    IMcpViewBuilder WithCsp(Action<IMcpCspBuilder> configure);

    /// <summary>
    /// Requests browser permissions for this view.
    /// </summary>
    IMcpViewBuilder WithPermissions(McpAppPermissions permissions);
}
