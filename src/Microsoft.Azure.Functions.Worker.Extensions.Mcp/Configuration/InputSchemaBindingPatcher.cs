// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Simple helper methods for extracting property types from input schemas and patching binding metadata.
/// </summary>
internal static class InputSchemaBindingPatcher
{
    /// <summary>
    /// Patches input binding metadata with property types extracted from the input schema.
    /// </summary>
    /// <param name="inputSchema">The input schema containing property definitions.</param>
    /// <param name="inputBindingProperties">The collection of input binding properties to patch.</param>
    public static void PatchBindingMetadata(
        JsonNode inputSchema,
        IEnumerable<ToolPropertyBinding> inputBindingProperties)
    {
        ArgumentNullException.ThrowIfNull(inputSchema);
        ArgumentNullException.ThrowIfNull(inputBindingProperties);

        var bindingList = inputBindingProperties.ToList();
        if (bindingList.Count == 0)
        {
            return;
        }

        var propertiesElement = GetPropertiesObject(inputSchema);

        foreach (var binding in bindingList)
        {
            if (TryGetPropertyType(propertiesElement, binding.PropertyName, out var propertyType))
            {
                binding.Binding[Constants.McpToolPropertyType] = propertyType;
            }
        }
    }

    /// <summary>
    /// Extracts the properties element from the input schema.
    /// </summary>
    private static JsonObject GetPropertiesObject(JsonNode inputSchema)
    {
        var schemaObject = inputSchema.AsObject();

        if (!schemaObject.TryGetPropertyValue("properties", out var propertiesNode))
        {
            throw new JsonException("Input schema does not contain a 'properties' element.");
        }

        if (propertiesNode is not JsonObject propertiesObject)
        {
            throw new InvalidOperationException("Input schema 'properties' element is not an object.");
        }

        return propertiesObject;
    }

    /// <summary>
    /// Attempts to get the property type for a given property name from the schema properties.
    /// </summary>
    private static bool TryGetPropertyType(JsonObject propertiesObject, string propertyName, out string? propertyType)
    {
        propertyType = null;

        if (!propertiesObject.TryGetPropertyValue(propertyName, out var propertySchema))
        {
            return false;
        }

        return TryExtractPropertyType(propertySchema, out propertyType);
    }

    /// <summary>
    /// Extracts the type from a property schema element.
    /// </summary>
    private static bool TryExtractPropertyType(JsonNode? propertySchema, out string? propertyType)
    {
        propertyType = null;

        if (propertySchema is not JsonObject schemaObject)
        {
            return false;
        }

        if (!schemaObject.TryGetPropertyValue("type", out var typeNode))
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

    /// <summary>
    /// Extracts the item type from an array property schema.
    /// </summary>
    private static bool TryGetArrayItemType(JsonObject propertySchema, out string? itemType)
    {
        itemType = null;

        if (!propertySchema.TryGetPropertyValue("items", out var itemsNode))
        {
            return false;
        }

        if (itemsNode is not JsonObject itemsObject)
        {
            return false;
        }

        if (!itemsObject.TryGetPropertyValue("type", out var itemTypeNode))
        {
            return false;
        }

        itemType = itemTypeNode?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(itemType);
    }
}

    /// <summary>
    /// Represents a tool property binding that can be patched with type information.
    /// </summary>
    /// <param name="PropertyName">The name of the property.</param>
    /// <param name="Binding">The JSON object representing the binding configuration.</param>
    public record ToolPropertyBinding(string PropertyName, JsonObject Binding);
