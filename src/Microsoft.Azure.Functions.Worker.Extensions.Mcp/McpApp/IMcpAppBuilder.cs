// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Configures an MCP App for a tool, including its view and visibility.
/// </summary>
public interface IMcpAppBuilder
{
    /// <summary>
    /// Configures the single view for this MCP App.
    /// </summary>
    IMcpAppBuilder WithView(Action<IMcpViewBuilder> configure);

    /// <summary>
    /// Sets the directory containing static assets (JS, CSS, images) to serve via HTTP.
    /// </summary>
    IMcpAppBuilder WithStaticAssets(string directory);

    /// <summary>
    /// Controls where the tool is visible (model, app, or both).
    /// Defaults to <see cref="McpVisibility.ModelAndApp"/>.
    /// </summary>
    IMcpAppBuilder WithVisibility(McpVisibility visibility);
}
