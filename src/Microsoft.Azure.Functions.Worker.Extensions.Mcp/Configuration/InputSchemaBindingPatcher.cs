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

        var propertiesElement = GetPropertiesElement(inputSchema);

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
    private static JsonElement GetPropertiesElement(JsonNode inputSchema)
    {
        var schemaString = inputSchema.ToJsonString();
        using var doc = JsonDocument.Parse(schemaString);
        var schema = doc.RootElement;

        if (!schema.TryGetProperty("properties", out var propertiesElement))
        {
            throw new InvalidOperationException("Input schema does not contain a 'properties' element.");
        }

        if (propertiesElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Input schema 'properties' element is not an object.");
        }

        return propertiesElement.Clone();
    }

    /// <summary>
    /// Attempts to get the property type for a given property name from the schema properties.
    /// </summary>
    private static bool TryGetPropertyType(JsonElement propertiesElement, string propertyName, out string? propertyType)
    {
        propertyType = null;

        if (!propertiesElement.TryGetProperty(propertyName, out var propertySchema))
        {
            return false;
        }

        return TryExtractPropertyType(propertySchema, out propertyType);
    }

    /// <summary>
    /// Extracts the type from a property schema element.
    /// </summary>
    private static bool TryExtractPropertyType(JsonElement propertySchema, out string? propertyType)
    {
        propertyType = null;

        if (!propertySchema.TryGetProperty("type", out var typeElement))
        {
            return false;
        }

        var typeString = typeElement.GetString();
        if (string.IsNullOrWhiteSpace(typeString))
        {
            return false;
        }

        // Handle array types - extract the item type for arrays
        if (typeString == "array")
        {
            return TryGetArrayItemType(propertySchema, out propertyType);
        }

        propertyType = typeString;
        return true;
    }

    /// <summary>
    /// Extracts the item type from an array property schema.
    /// </summary>
    private static bool TryGetArrayItemType(JsonElement propertySchema, out string? itemType)
    {
        itemType = null;

        if (!propertySchema.TryGetProperty("items", out var itemsElement))
        {
            return false;
        }

        if (!itemsElement.TryGetProperty("type", out var itemTypeElement))
        {
            return false;
        }

        itemType = itemTypeElement.GetString();
        return !string.IsNullOrWhiteSpace(itemType);
    }
}

/// <summary>
/// Represents a tool property binding that can be patched with type information.
/// </summary>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="Binding">The JSON object representing the binding configuration.</param>
public record ToolPropertyBinding(string PropertyName, JsonObject Binding);
