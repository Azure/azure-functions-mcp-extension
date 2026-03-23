// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring metadata and arguments of an MCP prompt within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and prompt options.</param>
/// <param name="promptName">The unique name of the prompt to configure.</param>
public sealed class McpPromptBuilder(IFunctionsWorkerApplicationBuilder builder, string promptName)
    : McpBuilderBase<PromptOptions, McpPromptBuilder>(builder, promptName)
{
    /// <summary>
    /// Adds an argument definition to the prompt configuration.
    /// </summary>
    /// <param name="name">The name of the argument. Cannot be null or empty.</param>
    /// <param name="description">A description of the argument.</param>
    /// <param name="required">Indicates whether the argument is required.</param>
    /// <returns>The current <see cref="McpPromptBuilder"/> instance, enabling fluent configuration.</returns>
    public McpPromptBuilder WithArgument(string name, string? description = null, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        Builder.Services.Configure<PromptOptions>(Name, o => o.AddArgument(name, description, required));

        return this;
    }
}
