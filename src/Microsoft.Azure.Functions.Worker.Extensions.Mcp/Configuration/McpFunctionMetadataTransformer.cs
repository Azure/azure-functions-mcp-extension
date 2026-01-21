// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.InputSchemaBindingPatcher;

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
            List<ToolProperty>? toolProperties = null;
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

            // Patch input binding metadata with type information
            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties, inputSchema);
        }
    }

    private bool TryGetConfiguredToolProperties(string? toolName, out List<ToolProperty>? toolProperties)
    {
        toolProperties = null;

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.Properties.Count != 0)
        {
            toolProperties = toolOptions.Properties;
            return true;
        }

        return false;
    }

    private static bool TryGenerateInputSchema(JsonObject jsonObject, IFunctionMetadata function, out JsonNode? inputSchema)
    {
        if (InputSchemaGenerator.TryGenerateFromFunction(function, out inputSchema) && inputSchema is not null)
        {
            jsonObject["inputSchema"] = inputSchema.ToJsonString();
            return true;
        }

        inputSchema = null;
        return false;
    }

    private static void PatchInputBindingMetadata(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties,
        List<ToolProperty>? toolProperties,
        JsonNode? inputSchema)
    {
        if (inputBindingProperties.Count == 0)
        {
            return;
        }

        // If we have toolProperties from IOptionsMonitor, use those
        if (toolProperties is not null && toolProperties.Count > 0)
        {
            PatchFromToolProperties(function, inputBindingProperties, toolProperties);
        }
        // Otherwise, use the generated input schema
        else if (inputSchema is not null)
        {
            InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, inputBindingProperties.Values);
            UpdateRawBindings(function, inputBindingProperties);
        }
    }

    private static void PatchFromToolProperties(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties,
        List<ToolProperty> toolProperties)
    {
        foreach (var property in toolProperties)
        {
            if (inputBindingProperties.TryGetValue(property.Name, out var binding))
            {
                binding.Binding[Constants.McpToolPropertyType] = property.Type;
            }
        }

        UpdateRawBindings(function, inputBindingProperties);
    }

    private static void UpdateRawBindings(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties)
    {
        foreach (var (propertyName, binding) in inputBindingProperties)
        {
            for (int i = 0; i < function.RawBindings!.Count; i++)
            {
                var rawBinding = function.RawBindings[i];
                var node = JsonNode.Parse(rawBinding);

                if (node is JsonObject jsonObj
                    && jsonObj.TryGetPropertyValue("type", out var typeNode)
                    && typeNode?.ToString() == Constants.McpToolPropertyBindingType
                    && jsonObj.TryGetPropertyValue(Constants.McpToolPropertyName, out var nameNode)
                    && nameNode?.ToString() == propertyName)
                {
                    function.RawBindings[i] = binding.Binding.ToJsonString();
                    break;
                }
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
}
