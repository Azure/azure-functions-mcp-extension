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
/// Extracts tool properties from function parameters and attributes.
/// </summary>
internal static class ToolPropertyParser
{
    /// <summary>
    /// Attempts to get tool properties from function attributes.
    /// </summary>
    public static bool TryGetPropertiesFromAttributes(IFunctionMetadata functionMetadata, out List<ToolProperty>? toolProperties)
    {
        FunctionMethodResolver.EnsureScriptRoot();
        
        if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method))
        {
            toolProperties = null;
            return false;
        }

        var parameters = method!.GetParameters();
        var properties = new List<ToolProperty>(capacity: parameters.Length);

        foreach (var parameter in parameters)
        {
            if (TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty))
            {
                properties.Add(toolProperty);
            }
            else if (TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var triggerProperties))
            {
                properties.AddRange(triggerProperties);
            }
        }

        toolProperties = properties;
        return true;
    }

    /// <summary>
    /// Serializes tool properties to a JSON node.
    /// </summary>
    public static JsonNode? GetPropertiesJson(List<ToolProperty> properties)
    {
        return JsonSerializer.Serialize(properties);
    }

    /// <summary>
    /// Attempts to extract a tool property from a parameter with McpToolPropertyAttribute.
    /// </summary>
    public static bool TryGetToolPropertyFromToolPropertyAttribute(ParameterInfo parameter, out ToolProperty toolProperty)
    {
        var toolAttribute = parameter.GetCustomAttribute<McpToolPropertyAttribute>();
        if (toolAttribute is null)
        {
            toolProperty = default!;
            return false;
        }

        McpToolPropertyType propertyType = parameter.ParameterType.MapToToolPropertyType();

        toolProperty = new(toolAttribute.PropertyName, propertyType.TypeName, toolAttribute.Description,
                           toolAttribute.IsRequired, propertyType.IsArray, propertyType.EnumValues);

        return true;
    }

    /// <summary>
    /// Attempts to extract tool properties from a POCO type on a trigger attribute parameter.
    /// </summary>
    public static bool TryGetToolPropertiesFromToolTriggerAttribute(ParameterInfo parameter, out List<ToolProperty> toolProperties)
    {
        toolProperties = [];

        var triggerAttribute = parameter.GetCustomAttribute<McpToolTriggerAttribute>();

        if (triggerAttribute is null
            || !parameter.ParameterType.IsPoco()
            || parameter.ParameterType == typeof(ToolInvocationContext))
        {
            return false;
        }

        foreach (var property in parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            McpToolPropertyType propertyType = property.PropertyType.MapToToolPropertyType();

            toolProperties.Add(new(property.Name, propertyType.TypeName, property.GetDescription(),
                                   property.IsRequired(), propertyType.IsArray, propertyType.EnumValues));
        }

        return toolProperties.Count > 0;
    }
}
