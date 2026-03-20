// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a base fluent builder for configuring metadata on MCP components within a Functions Worker application.
/// </summary>
/// <typeparam name="TOptions">The type of options to configure, which must derive from <see cref="McpBuilderOptions"/>.</typeparam>
/// <typeparam name="TBuilder">The derived builder type, used to enable fluent method chaining.</typeparam>
/// <param name="builder">The application builder used to configure services.</param>
/// <param name="name">The unique name of the component to configure.</param>
public abstract class McpBuilderBase<TOptions, TBuilder>(IFunctionsWorkerApplicationBuilder builder, string name)
    where TOptions : McpBuilderOptions
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
    public TBuilder WithMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        Builder.Services.Configure<TOptions>(Name, o => o.Metadata[key] = value);

        return (TBuilder)this;
    }

    /// <summary>
    /// Adds multiple metadata entries to the component configuration in a single operation.
    /// </summary>
    /// <param name="metadata">The metadata key-value pairs to add. Keys cannot be null or empty.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    public TBuilder WithMetadata(params KeyValuePair<string, object?>[] metadata)
    {
        foreach (var entry in metadata)
        {
            ArgumentException.ThrowIfNullOrEmpty(entry.Key, nameof(entry.Key));
        }

        Builder.Services.Configure<TOptions>(Name, o =>
        {
            foreach (var entry in metadata)
            {
                o.Metadata[entry.Key] = entry.Value;
            }
        });

        return (TBuilder)this;
    }
}
