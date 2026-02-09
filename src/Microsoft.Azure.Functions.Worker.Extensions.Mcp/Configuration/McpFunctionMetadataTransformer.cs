// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.InputSchemaBindingPatcher;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed partial class McpFunctionMetadataTransformer(IOptionsMonitor<ToolOptions> toolOptionsMonitor)
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
                        if (jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
                        {
                            var toolName = toolNameNode?.ToString();

                            if (TryGetConfiguredToolProperties(toolName, out toolProperties))
                            {
                                // Use configured tool properties from IOptionsMonitor
                                jsonObject["toolProperties"] = JsonSerializer.SerializeToNode(toolProperties);
                            }
                            else
                            {
                                // If not set in options monitor, use worker input schema approach
                                jsonObject["useWorkerInputSchema"] = true;

                                if (!TryGenerateInputSchema(jsonObject, function, out inputSchema))
                                {
                                    throw new InvalidOperationException(
                                        $"Failed to generate input schema for MCP tool trigger function '{function.Name}'.");
                                }
                            }
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            jsonObject["metadata"] = toolMetadataJson;
                        }

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
            UpdateRawBindings(function, inputBindingProperties);
        }
        // Otherwise, use the generated input schema
        else if (inputSchema is not null)
        {
            PatchBindingMetadata(inputSchema, inputBindingProperties);
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
    }

    private static void UpdateRawBindings(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties)
    {
        foreach (var (_, binding) in inputBindingProperties)
        {
            function.RawBindings![binding.Index] = binding.Binding.ToJsonString();
        }
    }
}
