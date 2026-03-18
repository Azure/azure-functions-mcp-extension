// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring properties of an MCP tool within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and tool options.</param>
/// <param name="toolName">The unique name of the tool to configure.</param>
public sealed class McpToolBuilder(IFunctionsWorkerApplicationBuilder builder, string toolName)
{
    [Obsolete($"Use the overload with an {nameof(McpToolPropertyType)} parameter.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public McpToolBuilder WithProperty(string name, string type, string description, bool required = false)
    {
        builder.Services.Configure<ToolOptions>(toolName, o => o.AddProperty(name, type, description, required));
        return this;
    }

    /// <summary>
    /// Adds a property definition to the tool configuration and returns the current builder instance for chaining.
    /// </summary>
    public McpToolBuilder WithProperty(string name, McpToolPropertyType type, string description, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        builder.Services.Configure<ToolOptions>(toolName, o => o.AddProperty(name, type, description, required));
        return this;
    }

    /// <summary>
    /// Configures this tool as an MCP App with a single HTML file.
    /// Generates a <c>ui://</c> resource and injects <c>_meta.ui</c> metadata automatically.
    /// </summary>
    /// <param name="filePath">Relative path to the HTML file to serve, resolved from the function app root.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance for chaining.</returns>
    public McpToolBuilder AsMcpApp(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        return AsMcpApp(app => app.WithView(view => view.FromFile(filePath)));
    }

    /// <summary>
    /// Configures this tool as an MCP App with full control over views, visibility, and assets.
    /// </summary>
    /// <param name="configure">An action that configures the MCP App via <see cref="IMcpAppBuilder"/>.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance for chaining.</returns>
    public McpToolBuilder AsMcpApp(Action<IMcpAppBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.Configure<ToolOptions>(toolName, o =>
        {
            var appBuilder = new McpAppBuilder(toolName);
            configure(appBuilder);
            o.AppOptions = appBuilder.Build();
        });
        return this;
    }
}
