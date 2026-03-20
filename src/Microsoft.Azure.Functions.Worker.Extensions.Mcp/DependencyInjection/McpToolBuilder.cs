// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

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

    private void EnsureMode(ConfigurationMode requested)
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
        EnsureMode(ConfigurationMode.Property);

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

        EnsureMode(ConfigurationMode.Property);

        Builder.Services.Configure<ToolOptions>(Name, o => o.AddProperty(name, type, description, required));

        return this;
    }

    /// <summary>
    /// Sets an explicit JSON input schema for the tool.
    /// Cannot be combined with <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/>.
    /// The schema must have root <c>"type": "object"</c> to be a valid MCP tool input schema.
    /// </summary>
    /// <param name="jsonSchema">A valid JSON schema string defining the tool's input parameters.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the schema is invalid JSON or does not conform to MCP requirements.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/> has already been called on this builder.</exception>
    public McpToolBuilder WithInputSchema(string jsonSchema)
    {
        ArgumentException.ThrowIfNullOrEmpty(jsonSchema, nameof(jsonSchema));

        EnsureMode(ConfigurationMode.InputSchema);

        var schemaNode = JsonNode.Parse(jsonSchema)
            ?? throw new ArgumentException("The provided JSON schema is not valid JSON.", nameof(jsonSchema));

        ValidateInputSchema(schemaNode);

        Builder.Services.Configure<ToolOptions>(Name, o => o.InputSchema = jsonSchema);

        return this;
    }

    /// <summary>
    /// Sets an explicit JSON input schema for the tool from a <see cref="JsonNode"/>.
    /// Cannot be combined with <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/>.
    /// The schema must have root <c>"type": "object"</c> to be a valid MCP tool input schema.
    /// </summary>
    /// <param name="schemaNode">A <see cref="JsonNode"/> representing a valid JSON schema defining the tool's input parameters.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schemaNode"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schema does not conform to MCP requirements.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/> has already been called on this builder.</exception>
    public McpToolBuilder WithInputSchema(JsonNode schemaNode)
    {
        ArgumentNullException.ThrowIfNull(schemaNode, nameof(schemaNode));

        EnsureMode(ConfigurationMode.InputSchema);

        ValidateInputSchema(schemaNode);

        var schemaJson = schemaNode.ToJsonString();
        Builder.Services.Configure<ToolOptions>(Name, o => o.InputSchema = schemaJson);

        return this;
    }

    /// <summary>
    /// Generates and sets a JSON input schema from the specified CLR type using <see cref="JsonSchemaExporter"/>.
    /// Cannot be combined with <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/>.
    /// The generated schema reflects the public properties and structure of the type.
    /// The type must produce a schema with root <c>"type": "object"</c> (i.e., a class/record with properties).
    /// </summary>
    /// <param name="type">The CLR type to generate the JSON schema from. Must be a class or record type.</param>
    /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> to control schema generation.
    /// When null, <see cref="JsonSerializerOptions.Default"/> is used.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when the generated schema does not have root <c>"type": "object"</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithProperty(string, McpToolPropertyType, string, bool)"/> has already been called on this builder.</exception>
    public McpToolBuilder WithInputSchema(Type type, JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        EnsureMode(ConfigurationMode.InputSchema);

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsAbstract)
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

        // Validate the generated schema conforms to MCP tool input schema requirements.
        // The host expects: root "type" = "object", optional "properties" (object), optional "required" (array).
        ValidateInputSchema(schemaNode, type);

        var schemaJson = schemaNode.ToJsonString();
        Builder.Services.Configure<ToolOptions>(Name, o => o.InputSchema = schemaJson);

        return this;
    }

    /// <summary>
    /// Generates and sets a JSON input schema from the specified CLR type <typeparamref name="T"/>
    /// using <see cref="JsonSchemaExporter"/>.
    /// </summary>
    /// <typeparam name="T">The CLR type to generate the JSON schema from.</typeparam>
    /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> to control schema generation.
    /// When null, <see cref="JsonSerializerOptions.Default"/> is used.</param>
    /// <returns>The current <see cref="McpToolBuilder"/> instance, enabling fluent configuration.</returns>
    public McpToolBuilder WithInputSchema<T>(JsonSerializerOptions? serializerOptions = null)
    {
        return WithInputSchema(typeof(T), serializerOptions);
    }

    /// <summary>
    /// Validates that the schema node conforms to MCP tool input schema requirements.
    /// The host expects: root "type" = "object", optional "properties" (object), optional "required" (array).
    /// </summary>
    private static void ValidateInputSchema(JsonNode schemaNode, Type? sourceType = null)
    {
        if (schemaNode is not JsonObject schemaObject)
        {
            throw new ArgumentException(
                FormatValidationError("Input schema must be a JSON object.", sourceType));
        }

        // Must have "type": "object" at root
        if (!schemaObject.TryGetPropertyValue("type", out var typeNode)
            || typeNode?.GetValue<string>() != "object")
        {
            throw new ArgumentException(
                FormatValidationError(
                    "Input schema must have root \"type\": \"object\". " +
                    "Ensure you are passing a class or record type with public properties, not a primitive type.",
                    sourceType));
        }

        // If "properties" exists, it should be an object
        if (schemaObject.TryGetPropertyValue("properties", out var propsNode)
            && propsNode is not null && propsNode is not JsonObject)
        {
            throw new ArgumentException(
                FormatValidationError("Input schema \"properties\" must be a JSON object.", sourceType));
        }

        // If "required" exists, it should be an array
        if (schemaObject.TryGetPropertyValue("required", out var reqNode)
            && reqNode is not null && reqNode is not JsonArray)
        {
            throw new ArgumentException(
                FormatValidationError("Input schema \"required\" must be a JSON array.", sourceType));
        }
    }

    private static string FormatValidationError(string message, Type? sourceType)
    {
        return sourceType is not null
            ? $"{message} Type: '{sourceType.FullName}'."
            : message;
    }
}
