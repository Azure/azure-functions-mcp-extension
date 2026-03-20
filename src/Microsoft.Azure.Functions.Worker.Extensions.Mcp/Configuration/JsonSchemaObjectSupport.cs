// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal static class JsonSchemaObjectSupport
{
    public static void ValidateObjectSchema(JsonNode schemaNode)
    {
        ArgumentNullException.ThrowIfNull(schemaNode);

        if (schemaNode is not JsonObject schemaObject)
        {
            throw new ArgumentException("Schema must be a JSON object.", nameof(schemaNode));
        }

        if (!schemaObject.TryGetPropertyValue("type", out var typeNode)
            || typeNode?.GetValue<string>() != "object")
        {
            throw new ArgumentException(
                "Schema must have root \"type\": \"object\". " +
                "Ensure you are passing a class or record type with public properties, not a primitive type.",
                nameof(schemaNode));
        }

        if (schemaObject.TryGetPropertyValue("properties", out var propertiesNode)
            && propertiesNode is not null && propertiesNode is not JsonObject)
        {
            throw new ArgumentException("Schema \"properties\" must be a JSON object.", nameof(schemaNode));
        }

        if (schemaObject.TryGetPropertyValue("required", out var requiredNode)
            && requiredNode is not null && requiredNode is not JsonArray)
        {
            throw new ArgumentException("Schema \"required\" must be a JSON array.", nameof(schemaNode));
        }
    }

    public static string GenerateObjectSchemaFromType(Type type, JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsAbstract || type.IsInterface)
        {
            throw new ArgumentException(
                $"Type '{type.FullName}' is not a valid schema source type. " +
                "The type must be a non-abstract class, record, or struct with public properties.",
                nameof(type));
        }

        var options = serializerOptions ?? SchemaExporterOptionsFactory.DefaultSerializerOptions;
        var schemaNode = options.GetJsonSchemaAsNode(type, SchemaExporterOptionsFactory.Create());

        try
        {
            ValidateObjectSchema(schemaNode);
        }
        catch (ArgumentException ex) when (ex.ParamName == nameof(schemaNode))
        {
            throw new ArgumentException($"Generated schema is invalid. {ex.Message}", nameof(type), ex);
        }

        return schemaNode.ToJsonString();
    }
}