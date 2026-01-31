// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring metadata of an MCP resource within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and resource options.</param>
/// <param name="resourceName">The unique name of the resource to configure.</param>
public sealed class McpResourceBuilder(IFunctionsWorkerApplicationBuilder builder, string resourceName)
{
    /// <summary>
    /// Adds a metadata entry to the resource configuration.
    /// </summary>
    /// <param name="key">The key for the metadata entry. Cannot be null or empty.</param>
    /// <param name="value">The value for the metadata entry.</param>
    /// <returns>The current <see cref="McpResourceBuilder"/> instance, enabling fluent configuration.</returns>
    public McpResourceBuilder WithMeta(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        builder.Services.Configure<ResourceOptions>(resourceName, o => o.AddMetadata(key, value));

        return this;
    }
}
