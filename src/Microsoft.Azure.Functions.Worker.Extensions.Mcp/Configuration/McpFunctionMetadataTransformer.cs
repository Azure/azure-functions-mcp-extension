// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;
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

                if (string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue("toolName", out var toolNameNode)
                    && GetToolProperties(toolNameNode?.ToString(), function, out toolProperties))
                {
                    jsonObject["toolProperties"] = ToolPropertyExtractor.GetPropertiesJson(toolProperties);
                    function.RawBindings[i] = jsonObject.ToJsonString();
                }
                else if (string.Equals(bindingType, McpResourceTriggerBindingType, StringComparison.OrdinalIgnoreCase)
                    && MetadataExtractor.TryGetResourceMetadata(function, out var resourceMetadata))
                {
                    jsonObject["metadata"] = MetadataExtractor.BuildMetadataJson(resourceMetadata);
                    function.RawBindings[i] = jsonObject.ToJsonString();
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
            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties);
        }
    }

    private static void PatchInputBindingMetadata(IFunctionMetadata function, Dictionary<string, ToolPropertyBinding> inputBindingProperties, List<ToolProperty>? toolProperties)
    {
        if (toolProperties is null
            || toolProperties.Count == 0
            || inputBindingProperties.Count == 0)
        {
            return;
        }

        foreach (var property in toolProperties)
        {
            if (inputBindingProperties.TryGetValue(property.Name, out var reference))
            {
                reference.Binding[Constants.McpToolPropertyType] = property.Type;
                function.RawBindings![reference.Index] = reference.Binding.ToJsonString();
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

        if (toolOptions.Properties.Count != 0)
        {
            toolProperties = toolOptions.Properties;
            return true;
        }

        return ToolPropertyExtractor.TryGetPropertiesFromAttributes(functionMetadata, out toolProperties);
    }

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
