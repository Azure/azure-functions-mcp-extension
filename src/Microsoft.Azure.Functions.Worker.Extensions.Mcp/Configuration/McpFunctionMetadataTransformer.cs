// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    IFunctionMethodResolver functionMethodResolver,
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
                            TryApplyMetadata(toolName, jsonObject, toolOptionsMonitor);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, functionMethodResolver, out var toolMetadataJson))
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

                        if (MetadataParser.TryGetResourceMetadata(function, functionMethodResolver, out var resourceMetadataJson))
                        {
                            ApplyOrMergeMetadata(jsonObject, resourceMetadataJson, "Resource", resourceUri);
                        }

                        function.RawBindings[i] = jsonObject.ToJsonString();
                        break;
                }
            }
        }
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

    /// <summary>
    /// Applies attributed metadata to the binding, merging with existing fluent API metadata if present.
    /// </summary>
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

    /// <summary>
    /// Merges two JSON metadata strings. Properties from the attributed metadata take precedence
    /// over fluent API metadata when keys overlap. Attributed metadata wins because attributes are
    /// declared directly on the function and are more explicit, whereas fluent API metadata is
    /// configured separately and is intended for defaults or shared configuration.
    /// </summary>
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
}
