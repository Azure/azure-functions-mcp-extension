// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Generates JSON schemas from function parameters using TypeExtensions reflection.
/// </summary>
internal static class InputSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema from function parameters.
    /// </summary>
    /// <param name="functionMetadata">The function metadata containing method information.</param>
    /// <param name="inputSchema">The generated JSON schema as a JsonNode if successful.</param>
    /// <returns>True if schema generation was successful, false otherwise.</returns>
    public static bool TryGenerateFromFunction(IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        inputSchema = null;

        if (!FunctionReflectionHelper.TryResolveMethod(functionMetadata, out var method) || method is null)
        {
            return false;
        }

        var parameters = method.GetParameters();
        inputSchema = GenerateFromParameters(parameters);
        return true;
    }

    /// <summary>
    /// Generates a JSON schema from method parameters.
    /// Excludes ToolInvocationContext and parameters with McpToolTriggerAttribute.
    /// Includes parameters with McpToolPropertyAttribute and POCO parameters.
    /// </summary>
    /// <param name="parameters">The method parameters to analyze.</param>
    /// <returns>A JSON schema JsonNode representing the parameters.</returns>
    public static JsonNode GenerateFromParameters(ParameterInfo[] parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            if (!ShouldIncludeParameter(parameter))
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
    /// Determines if a parameter should be included in schema generation.
    /// </summary>
    private static bool ShouldIncludeParameter(ParameterInfo parameter)
    {
        // Skip ToolInvocationContext parameters
        if (parameter.ParameterType == typeof(ToolInvocationContext))
        {
            return false;
        }

        // Skip parameters with McpToolTriggerAttribute
        if (parameter.GetCustomAttribute<McpToolTriggerAttribute>() is not null)
        {
            return false;
        }

        return true;
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

        var pocoProperties = GeneratePropertiesFromPoco(parameter.ParameterType);
        foreach (var kvp in pocoProperties.Properties)
        {
            properties[kvp.Key] = kvp.Value;
        }
        
        foreach (var requiredProp in pocoProperties.Required)
        {
            required.Add(requiredProp);
        }

        return true;
    }

    /// <summary>
    /// Generates properties and required list from a POCO type.
    /// </summary>
    /// <param name="pocoType">The POCO type to analyze.</param>
    /// <returns>A tuple containing the properties dictionary and required property names.</returns>
    private static (Dictionary<string, JsonNode> Properties, List<string> Required) GeneratePropertiesFromPoco(Type pocoType)
    {
        var properties = new Dictionary<string, JsonNode>();
        var required = new List<string>();

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

        return (properties, required);
    }

    /// <summary>
    /// Creates a property schema node for a single property.
    /// </summary>
    /// <param name="typeName">The JSON schema type name (string, number, integer, boolean, object).</param>
    /// <param name="description">The property description.</param>
    /// <param name="isArray">Whether the property is an array type.</param>
    /// <param name="enumValues">Optional enum values for the property.</param>
    /// <returns>A JsonNode representing the property schema.</returns>
    private static JsonNode CreatePropertySchema(string typeName, string description, bool isArray, IReadOnlyList<string> enumValues)
    {
        if (isArray)
        {
            var itemsSchema = new JsonObject
            {
                ["type"] = typeName
            };

            if (enumValues.Count > 0)
            {
                var enumArray = new JsonArray();
                foreach (var enumValue in enumValues)
                {
                    enumArray.Add(enumValue);
                }
                itemsSchema["enum"] = enumArray;
            }

            return new JsonObject
            {
                ["type"] = "array",
                ["description"] = description,
                ["items"] = itemsSchema
            };
        }
        else
        {
            var propertySchema = new JsonObject
            {
                ["type"] = typeName,
                ["description"] = description
            };

            if (enumValues.Count > 0)
            {
                var enumArray = new JsonArray();
                foreach (var enumValue in enumValues)
                {
                    enumArray.Add(enumValue);
                }
                propertySchema["enum"] = enumArray;
            }

            return propertySchema;
        }
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
