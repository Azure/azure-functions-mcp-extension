// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves and applies property types from input schemas to binding metadata.
/// </summary>
internal static class BindingTypeResolver
{
    /// <summary>
    /// Resolves property types from the input schema and applies them to the corresponding input binding metadata.
    /// </summary>
    public static void ResolveAndApplyTypes(
        JsonNode inputSchema,
        Dictionary<string, McpParsedBinding> inputBindingProperties)
    {
        if (inputBindingProperties.Count == 0)
        {
            return;
        }

        var propertiesObject = GetPropertiesObject(inputSchema);

        foreach (var (propertyName, binding) in inputBindingProperties)
        {
            if (TryGetPropertyType(propertiesObject, propertyName, out var propertyType))
            {
                binding.JsonObject[McpToolPropertyType] = propertyType;
            }
        }
    }

    private static JsonObject GetPropertiesObject(JsonNode inputSchema)
    {
        var schemaObject = inputSchema.AsObject();

        if (!schemaObject.TryGetPropertyValue("properties", out var propertiesNode)
            || propertiesNode is not JsonObject propertiesObject)
        {
            return [];
        }

        return propertiesObject;
    }

    private static bool TryGetPropertyType(JsonObject propertiesObject, string propertyName, out string? propertyType)
    {
        propertyType = null;

        if (!propertiesObject.TryGetPropertyValue(propertyName, out var propertySchema))
        {
            return false;
        }

        return TryExtractPropertyType(propertySchema, out propertyType);
    }

    private static bool TryExtractPropertyType(JsonNode? propertySchema, out string? propertyType)
    {
        propertyType = null;

        if (propertySchema is not JsonObject schemaObject
            || !schemaObject.TryGetPropertyValue("type", out var typeNode))
        {
            return false;
        }

        var typeString = typeNode?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(typeString))
        {
            return false;
        }

        if (typeString == "array")
        {
            return TryGetArrayItemType(schemaObject, out propertyType);
        }

        propertyType = typeString;
        return true;
    }

    private static bool TryGetArrayItemType(JsonObject propertySchema, out string? itemType)
    {
        itemType = null;

        if (!propertySchema.TryGetPropertyValue("items", out var itemsNode)
            || itemsNode is not JsonObject itemsObject
            || !itemsObject.TryGetPropertyValue("type", out var itemTypeNode))
        {
            return false;
        }

        itemType = itemTypeNode?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(itemType);
    }
}
