// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Configures an MCP App tool. Returned by <c>AsMcpApp()</c>.
/// </summary>
public interface IMcpAppBuilder
{
    /// <summary>
    /// Sets the view with the specified content source.
    /// </summary>
    IMcpViewBuilder WithView(McpViewSource source);

    /// <summary>
    /// Sets the view backed by a file.
    /// Shorthand for <c>WithView(McpViewSource.FromFile(filePath))</c>.
    /// Relative paths are resolved against the application's base directory
    /// (publish/output directory). Ensure the file is copied to the output directory
    /// (e.g., <c>&lt;Content Include="..." CopyToOutputDirectory="PreserveNewest" /&gt;</c>)
    /// so it is present both locally and when deployed.
    /// </summary>
    IMcpViewBuilder WithView(string filePath);

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
