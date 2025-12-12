// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed partial class McpFunctionMetadataTransformer()
    : IFunctionMetadataTransformer
{
    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        foreach (var function in original)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            JsonNode? inputSchema = null;
            Dictionary<string, ToolPropertyBinding> inputBindingProperties = [];

            for (int i = 0; i < function.RawBindings.Count; i++)
            {
                var binding = function.RawBindings[i];
                var node = JsonNode.Parse(binding);

                if (node is not JsonObject jsonObject
                    || !jsonObject.TryGetPropertyValue("type", out var bindingTypeNode))
                {
                    continue;
                }

                var bindingType = bindingTypeNode?.ToString();

                if (string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
                {
                    jsonObject["useWorkerInputSchema"] = true;

                    if (TryGenerateInputSchema(jsonObject, function, out inputSchema))
                    {
                        function.RawBindings[i] = jsonObject.ToJsonString();
                    }
                }
                else if (string.Equals(bindingType, McpToolPropertyBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue(McpToolPropertyName, out var propertyNameNode)
                    && propertyNameNode is not null)
                {
                    var propertyName = propertyNameNode.ToString();
                    inputBindingProperties.TryAdd(propertyName, new ToolPropertyBinding(i, jsonObject));
                }
            }

            // This is required for attributed properties/input bindings:
            PatchInputBindingMetadata(function, inputBindingProperties, inputSchema);
        }
    }

    /// <summary>
    /// Attempts to generate input schema from function parameters.
    /// </summary>
    private static bool TryGenerateInputSchema(JsonObject jsonObject, IFunctionMetadata function, out JsonNode? inputSchema)
    {
        if (InputSchemaGenerator.TryGenerateFromFunction(function, out inputSchema) && inputSchema is not null)
        {
            // Store the generated schema directly in the binding metadata
            jsonObject["inputSchema"] = inputSchema.ToJsonString();
            return true;
        }

        return false;
    }

    private static void PatchInputBindingMetadata(IFunctionMetadata function, Dictionary<string, ToolPropertyBinding> inputBindingProperties, JsonNode? inputSchema)
    {
        if (inputBindingProperties.Count == 0)
        {
            return;
        }

        // Try to get types from inputSchema
        if (inputSchema is not null)
        {
            try
            {
                // Parse inputSchema to get property types
                var schemaString = inputSchema.ToJsonString();
                using var doc = JsonDocument.Parse(schemaString);
                var schema = doc.RootElement;

                if (!doc.RootElement.TryGetProperty("properties", out var propertiesElement))
                {
                    return;
                }

                // For each input binding property, find its type in the schema
                foreach (var kvp in inputBindingProperties)
                {
                    var propertyName = kvp.Key;
                    var bindingRef = kvp.Value;

                    // Look for this property in the schema
                    if (propertiesElement.TryGetProperty(propertyName, out var propertySchema))
                    {
                        string? propertyType = null;

                        // Check if it's an array type
                        if (propertySchema.TryGetProperty("type", out var typeElement))
                        {
                            var typeStr = typeElement.GetString();
                            if (typeStr == "array")
                            {
                                // For arrays, get the item type
                                if (propertySchema.TryGetProperty("items", out var itemsElement) &&
                                    itemsElement.TryGetProperty("type", out var itemTypeElement))
                                {
                                    propertyType = itemTypeElement.GetString();
                                }
                            }
                            else
                            {
                                propertyType = typeStr;
                            }
                        }

                        // Patch the binding with the type
                        if (!string.IsNullOrEmpty(propertyType))
                        {
                            bindingRef.Binding[McpToolPropertyType] = propertyType;
                            function.RawBindings![bindingRef.Index] = bindingRef.Binding.ToJsonString();
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, skip patching
            }
        }
    }

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
