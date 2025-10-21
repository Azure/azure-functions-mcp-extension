// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// <param name="name">The name of the property to add. Cannot be null or empty.</param>
    /// <param name="type">The type of the property, specifying how the value will be interpreted.</param>
    /// <param name="description">A description of the property, used for documentation or help text. Cannot be null.</param>
    /// <param name="required">Indicates whether the property is required. If <see langword="true"/>, the property must be provided by the
    /// user.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    public McpToolBuilder WithProperty(string name, McpToolPropertyType type, string description, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        builder.Services.Configure<ToolOptions>(toolName, o => o.AddProperty(name, type, description, required));

        return this;
    }
}
