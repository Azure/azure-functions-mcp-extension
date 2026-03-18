// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Configures a single view within an MCP App. Returned by <c>WithView()</c>.
/// </summary>
public interface IMcpViewBuilder
{
    /// <summary>Sets the display title for this view.</summary>
    IMcpViewBuilder WithTitle(string title);

    /// <summary>
    /// Controls whether the host renders a border. Defaults to false.
    /// </summary>
    IMcpViewBuilder WithBorder(bool border = true);

    /// <summary>Sets the domain hint for cookie/storage scoping.</summary>
    IMcpViewBuilder WithDomain(string domain);

    /// <summary>Configures the Content Security Policy for this view.</summary>
    IMcpViewBuilder WithCsp(Action<IMcpCspBuilder> configure);

    /// <summary>Sets the permissions granted to this view.</summary>
    IMcpViewBuilder WithPermissions(McpAppPermissions permissions);

    /// <summary>
    /// Returns to the app builder to configure additional views or app-level settings.
    /// </summary>
    IMcpAppBuilder ConfigureApp();

    /// <summary>
    /// Returns to the tool builder to configure tool-level properties.
    /// </summary>
    McpToolBuilder ConfigureTool();
}
