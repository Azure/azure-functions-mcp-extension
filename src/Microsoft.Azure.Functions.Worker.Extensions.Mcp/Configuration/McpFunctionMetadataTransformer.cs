// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpFunctionMetadataTransformer(IOptionsMonitor<ToolOptions> toolOptionsMonitor)
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

                switch (bindingType)
                {
                    case McpToolTriggerBindingType:
                        if (jsonObject.TryGetPropertyValue("toolName", out var toolNameNode)
                            && GetToolProperties(toolNameNode?.ToString(), function, out toolProperties))
                        {
                            jsonObject["toolProperties"] = ToolPropertyParser.GetPropertiesJson(toolProperties);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            jsonObject["metadata"] = toolMetadataJson;
                        }

                    if (TryGenerateInputSchema(jsonObject, function, out inputSchema))
                    {
                        function.RawBindings[i] = jsonObject.ToJsonString();
                        break;

                    case McpResourceTriggerBindingType:
                        if (MetadataParser.TryGetResourceMetadata(function, out var resourceMetadataJson))
                        {
                            jsonObject["metadata"] = resourceMetadataJson;
                            function.RawBindings[i] = jsonObject.ToJsonString();
                        }
                        break;

                    case McpToolPropertyBindingType:
                        if (jsonObject.TryGetPropertyValue(McpToolPropertyName, out var propertyNameNode)
                            && propertyNameNode is not null)
                        {
                            var propertyName = propertyNameNode.ToString();
                            inputBindingProperties.TryAdd(propertyName, new ToolPropertyBinding(i, jsonObject));
                        }
                        break;
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

    private bool GetToolProperties(string? toolName, IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<ToolProperty>? toolProperties)
    {
        toolProperties = null;

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        // Get from configured options first:
        var toolOptions = toolOptionsMonitor.Get(toolName);

                continue;
            }

            McpToolPropertyType propertyType = property.PropertyType.MapToToolPropertyType();

            toolProperties.Add(new(property.Name, propertyType.TypeName, property.GetDescription(),
                                   property.IsRequired(), propertyType.IsArray, propertyType.EnumValues));
        }

        return toolProperties.Count > 0;
    }

    [GeneratedRegex(@"^(?<typename>.*)\.(?<methodname>\S*)$")]
    private static partial Regex GetEntryPointRegex();

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
