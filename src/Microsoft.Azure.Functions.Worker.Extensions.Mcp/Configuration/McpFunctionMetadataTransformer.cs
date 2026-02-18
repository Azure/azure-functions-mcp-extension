// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpFunctionMetadataTransformer(
    IOptionsMonitor<ToolOptions> toolOptionsMonitor,
    IOptionsMonitor<ResourceOptions> resourceOptionsMonitor)
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

                switch (bindingType)
                {
                    case McpToolTriggerBindingType:
                        bool hasFluentToolMetadata = false;
                        string? toolName = null;

                        if (jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
                        {
                            toolName = toolNameNode?.ToString();

                            if (GetToolProperties(toolName, function, out toolProperties))
                            {
                                jsonObject["toolProperties"] = ToolPropertyParser.GetPropertiesJson(toolProperties);
                            }

                            hasFluentToolMetadata = TryApplyMetadata(toolName, jsonObject, toolOptionsMonitor);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            if (hasFluentToolMetadata)
                            {
                                throw CreateDuplicateMetadataException("Tool", toolName);
                            }

                            jsonObject["metadata"] = toolMetadataJson;
                        }

                        function.RawBindings[i] = jsonObject.ToJsonString();
                        break;

                    case McpResourceTriggerBindingType:
                        bool hasFluentResourceMetadata = false;
                        string? resourceUri = null;

                        if (jsonObject.TryGetPropertyValue("uri", out var resourceUriNode))
                        {
                            resourceUri = resourceUriNode?.ToString();
                            hasFluentResourceMetadata = TryApplyMetadata(resourceUri, jsonObject, resourceOptionsMonitor);
                        }

                        if (MetadataParser.TryGetResourceMetadata(function, out var resourceMetadataJson))
                        {
                            if (hasFluentResourceMetadata)
                            {
                                throw CreateDuplicateMetadataException("Resource", resourceUri);
                            }

                            jsonObject["metadata"] = resourceMetadataJson;
                        }

                        function.RawBindings[i] = jsonObject.ToJsonString();
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

        return ToolPropertyParser.TryGetPropertiesFromAttributes(functionMetadata, out toolProperties);
    }

    private static bool TryApplyMetadata<TOptions>(string? name, JsonObject jsonObject, IOptionsMonitor<TOptions> optionsMonitor)
        where TOptions : class, IMcpBuilderOptions
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var options = optionsMonitor.Get(name);

        if (options.Metadata.Count > 0)
        {
            jsonObject["metadata"] = JsonSerializer.Serialize(options.Metadata);
            return true;
        }

        return false;
    }

    private static InvalidOperationException CreateDuplicateMetadataException(string type, string? identifier)
        => new($"{type} '{identifier}' has metadata defined using both the fluent API and [McpMetadata] attributes. " +
               $"Use only one approach to define metadata.");

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
