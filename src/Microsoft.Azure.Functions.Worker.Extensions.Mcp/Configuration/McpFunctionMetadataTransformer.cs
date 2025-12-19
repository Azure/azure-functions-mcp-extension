// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
                    // Use worker input schema approach
                    jsonObject["useWorkerInputSchema"] = true;

                    // Try to generate input schema from function parameters and throw an error if it fails
                    if (!TryGenerateInputSchema(jsonObject, function, out inputSchema))
                    {
                        throw new Exception($"Failed to generate input schema for MCP tool trigger function '{function.Name}'.");
                    }
                    
                    function.RawBindings[i] = jsonObject.ToJsonString();
                }
                else if (string.Equals(bindingType, McpToolPropertyBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue(McpToolPropertyName, out var propertyNameNode)
                    && propertyNameNode is not null)
                {
                    var propertyName = propertyNameNode.ToString();
                    var propertyBinding = new ToolPropertyBinding(propertyName, jsonObject);
                    inputBindingProperties.TryAdd(propertyName, propertyBinding);
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

        inputSchema = null;
        return false;
    }

    /// <summary>
    /// Patches input binding metadata with property types extracted from the input schema.
    /// Uses simple helper methods for better maintainability.
    /// </summary>
    private static void PatchInputBindingMetadata(
        IFunctionMetadata function, 
        Dictionary<string, ToolPropertyBinding> inputBindingProperties, 
        JsonNode? inputSchema)
    {
        if (inputBindingProperties.Count == 0)
        {
            return;
        }

        if (inputSchema is null)
        {
            return;
        }

        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, inputBindingProperties.Values);

        // Update the function's raw bindings with the patched binding objects
        foreach (var (propertyName, binding) in inputBindingProperties)
        {
            // Find the index of this binding in the raw bindings array
            for (int i = 0; i < function.RawBindings!.Count; i++)
            {
                var rawBinding = function.RawBindings[i];
                var node = JsonNode.Parse(rawBinding);
                    
                if (node is JsonObject jsonObj 
                    && jsonObj.TryGetPropertyValue("type", out var typeNode)
                    && typeNode?.ToString() == McpToolPropertyBindingType
                    && jsonObj.TryGetPropertyValue(McpToolPropertyName, out var nameNode)
                    && nameNode?.ToString() == propertyName)
                {
                    function.RawBindings[i] = binding.Binding.ToJsonString();
                    break;
                }
            }
        }
    }
}
