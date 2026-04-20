// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

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

    /// <summary>
    /// Sets an explicit JSON input schema for this MCP component.
    /// The schema must have root <c>"type": "object"</c> to be a valid MCP input schema.
    /// </summary>
    /// <param name="jsonSchema">A valid JSON schema string defining the component's input parameters.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is null/empty or does not conform to MCP requirements.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the schema string is not valid JSON.</exception>
    public virtual TBuilder WithInputSchema(string jsonSchema)
    {
        ArgumentException.ThrowIfNullOrEmpty(jsonSchema, nameof(jsonSchema));

        var schemaNode = JsonNode.Parse(jsonSchema)
            ?? throw new ArgumentException("The provided JSON schema could not be parsed as a JSON object.", nameof(jsonSchema));

        InputSchemaValidator.Validate(schemaNode);

        Builder.Services.Configure<TOptions>(Name, o => o.InputSchema = jsonSchema);

        return (TBuilder)this;
    }

    /// <summary>
    /// Sets an explicit JSON input schema for this MCP component from a <see cref="JsonNode"/>.
    /// The schema must have root <c>"type": "object"</c> to be a valid MCP input schema.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    public virtual TBuilder WithInputSchema(JsonNode schemaNode)
    {
        ArgumentNullException.ThrowIfNull(schemaNode, nameof(schemaNode));

        InputSchemaValidator.Validate(schemaNode);

        var schemaJson = schemaNode.ToJsonString();
        Builder.Services.Configure<TOptions>(Name, o => o.InputSchema = schemaJson);

        return (TBuilder)this;
    }

    /// <summary>
    /// Generates and sets a JSON input schema from the specified CLR type using <see cref="JsonSchemaExporter"/>.
    /// The type must produce a schema with root <c>"type": "object"</c> (i.e., a class/record with properties).
    /// </summary>
    /// <param name="type">The CLR type to generate the JSON schema from. Must be a class or record type.</param>
    /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> to control schema generation.
    /// When null, <see cref="JsonSerializerOptions.Default"/> is used.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the generated schema does not have root <c>"type": "object"</c>.</exception>
    public virtual TBuilder WithInputSchema(Type type, JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsAbstract || type.IsInterface)
        {
            throw new ArgumentException(
                $"Type '{type.FullName}' is not a valid input schema type. " +
                $"The type must be a non-abstract class, record, or struct with public properties.",
                nameof(type));
        }

        var options = serializerOptions ?? JsonSerializerOptions.Default;
        var schemaNode = options.GetJsonSchemaAsNode(type, new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
        });

        InputSchemaValidator.Validate(schemaNode, type);

        var schemaJson = schemaNode.ToJsonString();
        Builder.Services.Configure<TOptions>(Name, o => o.InputSchema = schemaJson);

        return (TBuilder)this;
    }

    /// <summary>
    /// Generates and sets a JSON input schema from the specified CLR type <typeparamref name="T"/>
    /// using <see cref="JsonSchemaExporter"/>.
    /// </summary>
    /// <typeparam name="T">The CLR type to generate the JSON schema from.</typeparam>
    /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> to control schema generation.
    /// When null, <see cref="JsonSerializerOptions.Default"/> is used.</param>
    /// <returns>The current builder instance, enabling fluent configuration.</returns>
    public TBuilder WithInputSchema<T>(JsonSerializerOptions? serializerOptions = null)
    {
        return WithInputSchema(typeof(T), serializerOptions);
    }
}
