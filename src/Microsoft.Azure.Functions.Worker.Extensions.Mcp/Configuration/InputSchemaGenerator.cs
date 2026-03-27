// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Generates JSON schemas from function parameters using reflection or tool properties.
/// </summary>
internal static class InputSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema from function parameters via reflection.
    /// </summary>
    public static bool TryGenerateFromFunction(IFunctionMetadata functionMetadata, out JsonNode? inputSchema, ILogger? logger = null)
    {
        inputSchema = null;

        try
        {
            if (!FunctionMethodResolver.TryGetScriptRoot(out _))
            {
                return false;
            }

            if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method) || method is null)
            {
                return false;
            }

            var parameters = method.GetParameters();
            inputSchema = GenerateFromParameters(parameters);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a JSON schema from method parameters.
    /// Excludes ToolInvocationContext and parameters with McpToolTriggerAttribute.
    /// Includes parameters with McpToolPropertyAttribute and POCO parameters.
    /// </summary>
    public static JsonNode GenerateFromParameters(ParameterInfo[] parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType == typeof(ToolInvocationContext))
            {
                continue;
            }

            if (TryProcessToolPropertyParameter(parameter, properties, required))
            {
                continue;
            }

            if (TryProcessPocoParameter(parameter, properties, required))
            {
                continue;
            }
        }

        return CreateSchemaNode(properties, required);
    }

    /// <summary>
    /// Processes parameters with McpToolPropertyAttribute.
    /// </summary>
    private static bool TryProcessToolPropertyParameter(ParameterInfo parameter, JsonObject properties, JsonArray required)
    {
        var toolPropertyAttr = parameter.GetCustomAttribute<McpToolPropertyAttribute>();
        if (toolPropertyAttr is null)
        {
            return false;
        }

        var propType = parameter.ParameterType.MapToToolPropertyType();
        properties[toolPropertyAttr.PropertyName] = CreatePropertySchema(
            propType.TypeName,
            toolPropertyAttr.Description ?? string.Empty,
            propType.IsArray,
            propType.EnumValues);

        if (toolPropertyAttr.IsRequired)
        {
            required.Add(toolPropertyAttr.PropertyName);
        }

        return true;
    }

    /// <summary>
    /// Processes POCO parameters (generates schema from all public properties).
    /// </summary>
    private static bool TryProcessPocoParameter(ParameterInfo parameter, JsonObject properties, JsonArray required)
    {
        if (!parameter.ParameterType.IsPoco())
        {
            return false;
        }

        var toolTriggerAttribute = parameter.GetCustomAttribute<McpToolTriggerAttribute>();
        if (toolTriggerAttribute is null)
        {
            return false;
        }

        GeneratePropertiesFromPoco(parameter.ParameterType, properties, required);
        return true;
    }

    /// <summary>
    /// Generates properties and required list from a POCO type.
    /// </summary>
    private static void GeneratePropertiesFromPoco(Type pocoType, JsonObject properties, JsonArray required)
    {
        foreach (var property in pocoType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            var propType = property.PropertyType.MapToToolPropertyType();
            var description = property.GetDescription() ?? string.Empty;

            properties[property.Name] = CreatePropertySchema(
                propType.TypeName,
                description,
                propType.IsArray,
                propType.EnumValues);

            if (property.IsRequired())
            {
                required.Add(property.Name);
            }
        }
    }

    /// <summary>
    /// Creates a property schema node for a single property.
    /// </summary>
    private static JsonNode CreatePropertySchema(string typeName, string description, bool isArray, IReadOnlyList<string> enumValues)
    {
        var schema = new JsonObject
        {
            ["type"] = isArray ? "array" : typeName,
            ["description"] = description
        };

        if (isArray)
        {
            var itemsSchema = new JsonObject { ["type"] = typeName };

            if (enumValues.Count > 0)
            {
                itemsSchema["enum"] = CreateEnumArray(enumValues);
            }

            schema["items"] = itemsSchema;
        }
        else if (enumValues.Count > 0)
        {
            schema["enum"] = CreateEnumArray(enumValues);
        }

        return schema;
    }

    private static JsonArray CreateEnumArray(IReadOnlyList<string> enumValues)
    {
        var enumArray = new JsonArray();
        foreach (var enumValue in enumValues)
        {
            enumArray.Add(enumValue);
        }
        return enumArray;
    }

    /// <summary>
    /// Generates a JSON schema from a list of <see cref="ToolProperty"/> objects.
    /// Used when tool properties are configured via the fluent API (WithProperty).
    /// </summary>
    public static JsonNode GenerateFromToolProperties(List<ToolProperty> toolProperties)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var toolProperty in toolProperties)
        {
            properties[toolProperty.Name] = CreatePropertySchema(
                toolProperty.Type,
                toolProperty.Description ?? string.Empty,
                toolProperty.IsArray,
                toolProperty.EnumValues);

            if (toolProperty.IsRequired)
            {
                required.Add(toolProperty.Name);
            }
        }

        return CreateSchemaNode(properties, required);
    }

    /// <summary>
    /// Creates the final JSON schema JsonNode from properties and required arrays.
    /// </summary>
    private static JsonNode CreateSchemaNode(JsonObject properties, JsonArray required)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };
    }
}
