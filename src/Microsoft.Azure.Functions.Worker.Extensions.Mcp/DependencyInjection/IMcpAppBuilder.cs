// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Configures an MCP App tool. Returned by <c>AsMcpApp()</c>.
/// </summary>
public interface IMcpAppBuilder
{
    /// <summary>
    /// Adds a default (unnamed) view with the specified content source.
    /// </summary>
    IMcpViewBuilder WithView(McpViewSource source);

    /// <summary>
    /// Adds a named view with the specified content source.
    /// </summary>
    IMcpViewBuilder WithView(string viewName, McpViewSource source);

    /// <summary>
    /// Adds a default (unnamed) view backed by a file.
    /// Shorthand for <c>WithView(McpViewSource.FromFile(filePath))</c>.
    /// </summary>
    IMcpViewBuilder WithView(string filePath);

    /// <summary>
    /// Adds a named view backed by a file.
    /// Shorthand for <c>WithView(viewName, McpViewSource.FromFile(filePath))</c>.
    /// </summary>
    IMcpViewBuilder WithView(string viewName, string filePath);

    /// <summary>
    /// Configures the directory from which static assets are served.
    /// </summary>
    IMcpAppBuilder WithStaticAssets(string directory);

    /// <summary>
    /// Configures the directory and options for static asset serving.
    /// </summary>
    IMcpAppBuilder WithStaticAssets(string directory, Action<StaticAssetOptions> configure);

    /// <summary>
    /// Sets the visibility of this tool. Last call wins.
    /// </summary>
    IMcpAppBuilder WithVisibility(McpVisibility visibility);
}
