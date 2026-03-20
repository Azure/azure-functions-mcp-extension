// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal static class ToolPropertyExtractor
{
    public static bool TryExtractFromAttributes(IFunctionMetadata functionMetadata, out List<ToolProperty> toolProperties)
    {
        toolProperties = [];

        if (!FunctionReflectionHelper.TryResolveMethodStrict(functionMetadata, out var method) || method is null)
        {
            return false;
        }

        var parameters = method.GetParameters();
        var properties = new List<ToolProperty>(parameters.Length);

        foreach (var parameter in parameters)
        {
            if (TryGetToolPropertyFromAttribute(parameter, out var toolProperty))
            {
                properties.Add(toolProperty);
            }
            else if (TryGetToolPropertiesFromTriggerAttribute(parameter, out var triggerProperties))
            {
                properties.AddRange(triggerProperties);
            }
        }

        toolProperties = properties;
        return true;
    }

    private static bool TryGetToolPropertyFromAttribute(ParameterInfo parameter, out ToolProperty toolProperty)
    {
        var toolAttribute = parameter.GetCustomAttribute<McpToolPropertyAttribute>();
        if (toolAttribute is null)
        {
            toolProperty = default!;
            return false;
        }

        McpToolPropertyType propertyType = parameter.ParameterType.MapToToolPropertyType();

        toolProperty = new(
            toolAttribute.PropertyName,
            propertyType.TypeName,
            toolAttribute.Description,
            toolAttribute.IsRequired,
            propertyType.IsArray,
            propertyType.EnumValues);

        return true;
    }

    private static bool TryGetToolPropertiesFromTriggerAttribute(ParameterInfo parameter, out List<ToolProperty> toolProperties)
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
            toolProperties.Add(new(
                property.Name,
                propertyType.TypeName,
                property.GetDescription(),
                property.IsRequired(),
                propertyType.IsArray,
                propertyType.EnumValues));
        }

        return toolProperties.Count > 0;
    }
}