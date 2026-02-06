// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a base fluent builder for configuring metadata on MCP components within a Functions Worker application.
/// </summary>
/// <typeparam name="TOptions">The type of options to configure, which must implement <see cref="IMcpBuilderOptions"/>.</typeparam>
/// <typeparam name="TBuilder">The derived builder type, used to enable fluent method chaining.</typeparam>
/// <param name="builder">The application builder used to configure services.</param>
/// <param name="name">The unique name of the component to configure.</param>
public abstract class McpBuilderBase<TOptions, TBuilder>(IFunctionsWorkerApplicationBuilder builder, string name)
    where TOptions : class, IMcpBuilderOptions
    where TBuilder : McpBuilderBase<TOptions, TBuilder>
{
    /// <summary>
    /// Gets the application builder used to configure services.
    /// </summary>
    protected IFunctionsWorkerApplicationBuilder Builder => builder;

    /// <summary>
    /// Gets the unique name of the component being configured.
    /// </summary>
    protected string Name => name;

    /// <summary>
    /// Adds a metadata entry to the component configuration.
    /// </summary>
    /// <param name="key">The key for the metadata entry. Cannot be null or empty.</param>
    /// <param name="value">The value for the metadata entry.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    public TBuilder WithMeta(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        Builder.Services.Configure<TOptions>(Name, o => o.AddMetadata(key, value));

        return (TBuilder)this;
    }
}
