// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
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
    [Obsolete($"Use the overload with an {nameof(McpToolPropertyType)} parameter.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public McpToolBuilder WithProperty(string name, string type, string description, bool required = false)
    {
        Builder.Services.Configure<ToolOptions>(Name, o => o.AddProperty(name, type, description, required));

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

        Builder.Services.Configure<ToolOptions>(Name, o => o.AddProperty(name, type, description, required));

        return this;
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

    /// <summary>
    /// Sets an explicit JSON input schema for this MCP tool. The schema is validated
    /// to ensure it has root <c>"type": "object"</c> (and that <c>properties</c>/<c>required</c>
    /// have the correct shapes when present). When set, the worker emits this schema and
    /// signals the host (via <c>useWorkerInputSchema = true</c>) to use it instead of
    /// generating one from <c>toolProperties</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="WithInputSchema(string)"/> may be combined with <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/>;
    /// the explicit schema takes precedence on hosts that understand it, while older hosts
    /// continue to consume <c>toolProperties</c>.
    /// </remarks>
    /// <param name="jsonSchema">A valid JSON schema string defining the tool's input parameters.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is null/empty or does not conform to MCP requirements.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the schema string is not valid JSON.</exception>
    public McpToolBuilder WithInputSchema(string jsonSchema)
    {
        var schema = new McpInputSchema(jsonSchema);

        Builder.Services.Configure<ToolOptions>(Name, o => o.InputSchema = schema);

        return this;
    }

    /// <summary>
    /// Sets an explicit JSON input schema for this MCP tool from a <see cref="JsonNode"/>.
    /// See <see cref="WithInputSchema(string)"/> for details.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    public McpToolBuilder WithInputSchema(JsonNode schemaNode)
    {
        var schema = new McpInputSchema(schemaNode);

        Builder.Services.Configure<ToolOptions>(Name, o => o.InputSchema = schema);

        return this;
    }

    /// <summary>
    /// Sets an explicit JSON output schema for this MCP tool. The schema is validated
    /// to ensure it has root <c>"type": "object"</c> (and that <c>properties</c>/<c>required</c>
    /// have the correct shapes when present). When set, the worker emits this schema on
    /// the tool trigger binding; the host advertises it on <c>tools/list</c>.
    /// </summary>
    /// <param name="jsonSchema">A valid JSON schema string defining the tool's output structure.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is null/whitespace or does not conform to MCP requirements.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the schema string is not valid JSON.</exception>
    public McpToolBuilder WithOutputSchema(string jsonSchema)
    {
        var schema = new McpOutputSchema(jsonSchema);

        Builder.Services.Configure<ToolOptions>(Name, o => o.OutputSchema = schema);

        return this;
    }

    /// <summary>
    /// Sets an explicit JSON output schema for this MCP tool from a <see cref="JsonNode"/>.
    /// See <see cref="WithOutputSchema(string)"/> for details.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    public McpToolBuilder WithOutputSchema(JsonNode schemaNode)
    {
        var schema = new McpOutputSchema(schemaNode);

        Builder.Services.Configure<ToolOptions>(Name, o => o.OutputSchema = schema);

        return this;
    }
}
