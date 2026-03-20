// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal sealed class McpFunctionMetadataTransformer(
    IOptionsMonitor<ToolOptions> toolOptionsMonitor,
    IOptionsMonitor<ResourceOptions> resourceOptionsMonitor,
    ILogger<McpFunctionMetadataTransformer> logger)
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
                        string? toolName = null;

                        if (jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
                        {
                            toolName = toolNameNode?.ToString();
                            TryProcessToolTriggerBinding(jsonObject, function, toolName, out toolProperties, out inputSchema);
                            TryApplyMetadata(toolName, jsonObject, toolOptionsMonitor);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            ApplyOrMergeMetadata(jsonObject, toolMetadataJson, "Tool", toolName);
                        }
                        function.RawBindings[i] = jsonObject.ToJsonString();
                        break;

                    case McpResourceTriggerBindingType:
                        string? resourceUri = null;

                        if (jsonObject.TryGetPropertyValue("uri", out var resourceUriNode))
                        {
                            resourceUri = resourceUriNode?.ToString();
                            TryApplyMetadata(resourceUri, jsonObject, resourceOptionsMonitor);
                        }

                        if (MetadataParser.TryGetResourceMetadata(function, out var resourceMetadataJson))
                        {
                            ApplyOrMergeMetadata(jsonObject, resourceMetadataJson, "Resource", resourceUri);
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

            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties, inputSchema);
        }
    }

    private bool TryProcessToolTriggerBinding(JsonObject jsonObject, IFunctionMetadata function, string? toolName, out List<ToolProperty>? toolProperties, out JsonNode? inputSchema)
    {
        toolProperties = null;
        inputSchema = null;

        bool useWorkerInputSchema = jsonObject.TryGetPropertyValue("useWorkerInputSchema", out var useInputSchemaNode)
            && useInputSchemaNode is not null
            && useInputSchemaNode.GetValue<bool>();

        if (useWorkerInputSchema)
        {
            return TryGenerateInputSchema(jsonObject, function, out inputSchema);
        }

        return TryGenerateToolProperties(jsonObject, function, toolName, out toolProperties);
    }

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

    private bool TryGenerateToolProperties(JsonObject jsonObject, IFunctionMetadata function, string? toolName, out List<ToolProperty>? toolProperties)
    {
        if (GetToolProperties(toolName, function, out toolProperties))
        {
            jsonObject["toolProperties"] = ToolPropertyParser.GetPropertiesJson(toolProperties);
            return true;
        }

        return false;
    }

    private static void PatchInputBindingMetadata(IFunctionMetadata function, Dictionary<string, ToolPropertyBinding> inputBindingProperties, List<ToolProperty>? toolProperties, JsonNode? inputSchema)
    {
        if (inputBindingProperties.Count == 0)
        {
            return;
        }

        if (toolProperties is not null && toolProperties.Count > 0)
        {
            foreach (var property in toolProperties)
            {
                if (inputBindingProperties.TryGetValue(property.Name, out var reference))
                {
                    reference.Binding[McpToolPropertyType] = property.Type;
                    function.RawBindings![reference.Index] = reference.Binding.ToJsonString();
                }
            }

            return;
        }

        if (inputSchema is null)
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(inputSchema.ToJsonString());
            var schema = doc.RootElement;

            if (!schema.TryGetProperty("properties", out var propertiesElement))
            {
                return;
            }

            foreach (var kvp in inputBindingProperties)
            {
                var propertyName = kvp.Key;
                var bindingRef = kvp.Value;

                if (!propertiesElement.TryGetProperty(propertyName, out var propertySchema))
                {
                    continue;
                }

                string? propertyType = null;

                if (propertySchema.TryGetProperty("type", out var typeElement))
                {
                    var typeStr = typeElement.GetString();
                    if (typeStr == "array")
                    {
                        if (propertySchema.TryGetProperty("items", out var itemsElement)
                            && itemsElement.TryGetProperty("type", out var itemTypeElement))
                        {
                            propertyType = itemTypeElement.GetString();
                        }
                    }
                    else
                    {
                        propertyType = typeStr;
                    }
                }

                if (!string.IsNullOrEmpty(propertyType))
                {
                    bindingRef.Binding[McpToolPropertyType] = propertyType;
                    function.RawBindings![bindingRef.Index] = bindingRef.Binding.ToJsonString();
                }
            }
        }
        catch
        {
            // If parsing fails, skip patching.
        }
    }

    private bool GetToolProperties(string? toolName, IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<ToolProperty>? toolProperties)
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

        if (ToolPropertyExtractor.TryExtractFromAttributes(functionMetadata, out var attributedToolProperties))
        {
            toolProperties = attributedToolProperties;
            return true;
        }

        return false;
    }
    private static void TryApplyMetadata<TOptions>(string? name, JsonObject jsonObject, IOptionsMonitor<TOptions> optionsMonitor)
        where TOptions : McpBuilderOptions
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var options = optionsMonitor.Get(name);

        if (options.Metadata.Count > 0)
        {
            jsonObject["metadata"] = JsonSerializer.Serialize(options.Metadata);
        }
    }

    private void ApplyOrMergeMetadata(JsonObject jsonObject, string attributedMetadataJson, string type, string? identifier)
    {
        if (jsonObject.ContainsKey("metadata"))
        {
            jsonObject["metadata"] = MergeMetadata(jsonObject["metadata"]?.GetValue<string>(), attributedMetadataJson, out var overlappingKeys);
            logger.LogTrace("{Type} '{Identifier}' has metadata defined using both the fluent API and [McpMetadata] attributes. Metadata from both sources has been merged.", type, identifier);

            if (overlappingKeys.Count > 0)
            {
                logger.LogDebug("{Type} '{Identifier}' has overlapping metadata keys: {Keys}. Values from [McpMetadata] attributes will be used.", type, identifier, string.Join(", ", overlappingKeys));
            }
        }
        else
        {
            jsonObject["metadata"] = attributedMetadataJson;
        }
    }

    internal static string MergeMetadata(string? fluentJson, string? attributedJson, out List<string> overlappingKeys)
    {
        var fluentNode = string.IsNullOrWhiteSpace(fluentJson)
            ? []
            : JsonNode.Parse(fluentJson)?.AsObject() ?? throw new InvalidOperationException($"Failed to parse fluent API metadata as JSON object: {fluentJson}");
        var attributedNode = string.IsNullOrWhiteSpace(attributedJson)
            ? []
            : JsonNode.Parse(attributedJson)?.AsObject() ?? throw new InvalidOperationException($"Failed to parse attributed metadata as JSON object: {attributedJson}");

        overlappingKeys = [];

        foreach (var property in attributedNode)
        {
            if (fluentNode.ContainsKey(property.Key))
            {
                overlappingKeys.Add(property.Key);
            }

            fluentNode[property.Key] = property.Value?.DeepClone();
        }

        return fluentNode.ToJsonString();
    }

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
