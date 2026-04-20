// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Generates JSON schemas from function parameters using reflection or tool/prompt properties.
/// </summary>
internal static class InputSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema from tool function parameters via reflection.
    /// </summary>
    public static bool TryGenerateFromToolFunction(IFunctionMetadata functionMetadata, out JsonNode? inputSchema, ILogger? logger = null)
    {
        return TryGenerateFromFunction(functionMetadata, GenerateFromToolParameters, out inputSchema, logger);
    }

    /// <summary>
    /// Generates a JSON schema from prompt function parameters via reflection.
    /// </summary>
    public static bool TryGenerateFromPromptFunction(IFunctionMetadata functionMetadata, out JsonNode? inputSchema, ILogger? logger = null)
    {
        return TryGenerateFromFunction(functionMetadata, GenerateFromPromptParameters, out inputSchema, logger);
    }

    private static bool TryGenerateFromFunction(
        IFunctionMetadata functionMetadata,
        Func<ParameterInfo[], JsonNode> generator,
        out JsonNode? inputSchema,
        ILogger? logger = null)
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
            inputSchema = generator(parameters);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to generate input schema from function '{FunctionName}' via reflection.", functionMetadata.Name);
            return false;
        }
    }

    /// <summary>
    /// Generates a JSON schema from tool method parameters.
    /// Excludes ToolInvocationContext and parameters with McpToolTriggerAttribute.
    /// Includes parameters with McpToolPropertyAttribute and POCO parameters.
    /// </summary>
    public static JsonNode GenerateFromToolParameters(ParameterInfo[] parameters)
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

            if (TryProcessToolPocoParameter(parameter, properties, required))
            {
                continue;
            }
        }

        return CreateSchemaNode(properties, required);
    }

    /// <summary>
    /// Generates a JSON schema from prompt method parameters.
    /// Excludes PromptInvocationContext.
    /// Includes parameters with McpPromptArgumentAttribute and POCO parameters.
    /// </summary>
    public static JsonNode GenerateFromPromptParameters(ParameterInfo[] parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType == typeof(PromptInvocationContext))
            {
                continue;
            }

            if (TryProcessPromptArgumentParameter(parameter, properties, required))
            {
                continue;
            }

            if (TryProcessPromptPocoParameter(parameter, properties, required))
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
    /// Processes parameters with McpPromptArgumentAttribute.
    /// </summary>
    private static bool TryProcessPromptArgumentParameter(ParameterInfo parameter, JsonObject properties, JsonArray required)
    {
        var promptArgAttr = parameter.GetCustomAttribute<McpPromptArgumentAttribute>();
        if (promptArgAttr is null)
        {
            return false;
        }

        properties[promptArgAttr.ArgumentName] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = promptArgAttr.Description ?? string.Empty
        };

        if (promptArgAttr.IsRequired)
        {
            required.Add(promptArgAttr.ArgumentName);
        }

        return true;
    }

    /// <summary>
    /// Processes POCO parameters with McpToolTriggerAttribute.
    /// </summary>
    private static bool TryProcessToolPocoParameter(ParameterInfo parameter, JsonObject properties, JsonArray required)
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
    /// Processes POCO parameters with McpPromptTriggerAttribute.
    /// </summary>
    private static bool TryProcessPromptPocoParameter(ParameterInfo parameter, JsonObject properties, JsonArray required)
    {
        if (!parameter.ParameterType.IsPoco())
        {
            return false;
        }

        var promptTriggerAttribute = parameter.GetCustomAttribute<McpPromptTriggerAttribute>();
        if (promptTriggerAttribute is null)
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
    /// Generates a JSON schema from a list of <see cref="PromptArgumentDefinition"/> objects.
    /// </summary>
    public static JsonNode GenerateFromPromptArguments(List<PromptArgumentDefinition> arguments)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var argument in arguments)
        {
            properties[argument.Name] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = argument.Description ?? string.Empty
            };

            if (argument.Required)
            {
                required.Add(argument.Name);
            }
        }

        return CreateSchemaNode(properties, required);
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
