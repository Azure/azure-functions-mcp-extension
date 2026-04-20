// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring properties of an MCP tool within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and tool options.</param>
/// <param name="toolName">The unique name of the tool to configure.</param>
public sealed class McpToolBuilder(IFunctionsWorkerApplicationBuilder builder, string toolName)
    : McpBuilderBase<ToolOptions, McpToolBuilder>(builder, toolName)
{
    private enum ConfigurationMode
    {
        None,
        Property,
        InputSchema
    }

    private ConfigurationMode _mode = ConfigurationMode.None;

    private void EnsureSchemaConfigurationMode(ConfigurationMode requested)
    {
        if (_mode != ConfigurationMode.None && _mode != requested)
        {
            var current = _mode == ConfigurationMode.Property ? nameof(WithProperty) : nameof(WithInputSchema);
            var attempted = requested == ConfigurationMode.Property ? nameof(WithProperty) : nameof(WithInputSchema);
            throw new InvalidOperationException(
                $"Cannot use {attempted} when {current} has already been called. " +
                $"{nameof(WithProperty)} and {nameof(WithInputSchema)} are mutually exclusive.");
        }

        _mode = requested;
    }

    [Obsolete($"Use the overload with an {nameof(McpToolPropertyType)} parameter.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public McpToolBuilder WithProperty(string name, string type, string description, bool required = false)
    {
        EnsureSchemaConfigurationMode(ConfigurationMode.Property);

        Builder.Services.Configure<ToolOptions>(Name, o => o.AddProperty(name, type, description, required));

        return this;
    }

    /// <summary>
    /// Adds a property definition to the tool configuration and returns the current builder instance for chaining.
    /// Cannot be combined with <see cref="WithInputSchema(string)"/> or <see cref="WithInputSchema(Type, JsonSerializerOptions?)"/>.
    /// </summary>
    /// <param name="name">The name of the property to add. Cannot be null or empty.</param>
    /// <param name="type">The type of the property, specifying how the value will be interpreted.</param>
    /// <param name="description">A description of the property, used for documentation or help text. Cannot be null.</param>
    /// <param name="required">Indicates whether the property is required. If <see langword="true"/>, the property must be provided by the
    /// user.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithInputSchema(string)"/> has already been called on this builder.</exception>
    public McpToolBuilder WithProperty(string name, McpToolPropertyType type, string description, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        EnsureSchemaConfigurationMode(ConfigurationMode.Property);

        Builder.Services.Configure<ToolOptions>(Name, o => o.AddProperty(name, type, description, required));

        return this;
    }

    /// <inheritdoc />
    public override McpToolBuilder WithInputSchema(string jsonSchema)
    {
        EnsureSchemaConfigurationMode(ConfigurationMode.InputSchema);
        return base.WithInputSchema(jsonSchema);
    }

    /// <inheritdoc />
    public override McpToolBuilder WithInputSchema(JsonNode schemaNode)
    {
        EnsureSchemaConfigurationMode(ConfigurationMode.InputSchema);
        return base.WithInputSchema(schemaNode);
    }

    /// <inheritdoc />
    public override McpToolBuilder WithInputSchema(Type type, JsonSerializerOptions? serializerOptions = null)
    {
        EnsureSchemaConfigurationMode(ConfigurationMode.InputSchema);
        return base.WithInputSchema(type, serializerOptions);
    }

    /// <summary>
    /// Configures this tool as an MCP App with a UI view.
    /// </summary>
    /// <param name="configure">A delegate to configure the app settings.</param>
    /// <returns>The tool builder for further chaining.</returns>
    public McpToolBuilder AsMcpApp(Action<IMcpAppBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        Builder.Services.Configure<ToolOptions>(Name, toolOptions =>
        {
            toolOptions.AppOptions ??= new AppOptions();
            var appBuilder = new McpAppBuilder(toolOptions.AppOptions, this);

            configure(appBuilder);
        });

        return this;
    }
}
