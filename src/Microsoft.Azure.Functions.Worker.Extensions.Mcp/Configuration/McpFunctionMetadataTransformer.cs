// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal sealed class McpFunctionMetadataTransformer(
    IOptionsMonitor<ToolOptions> toolOptionsMonitor,
    IOptionsMonitor<ResourceOptions> resourceOptionsMonitor,
    ILogger<McpFunctionMetadataTransformer>? logger = null)
    : IFunctionMetadataTransformer
{
    private readonly ILogger _logger = logger ?? (ILogger)NullLogger.Instance;

    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        List<DefaultFunctionMetadata>? syntheticFunctions = null;

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
                        string? toolName = null;

                        if (jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
                        {
                            toolName = toolNameNode?.ToString();

                            if (GetToolProperties(toolName, function, out toolProperties))
                            {
                                jsonObject["toolProperties"] = ToolPropertyParser.GetPropertiesJson(toolProperties);
                            }

                            TryApplyMetadata(toolName, jsonObject, toolOptionsMonitor);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            ApplyOrMergeMetadata(jsonObject, toolMetadataJson, "Tool", toolName);
                        }

                        // Merge MCP App UI metadata and emit synthetic HTTP functions
                        MergeAppUiMetadata(jsonObject, toolNameNode?.ToString(), ref syntheticFunctions);

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

            // This is required for attributed properties/input bindings:
            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties);
        }

        // Add synthetic functions after iteration to avoid modifying collection during enumeration
        if (syntheticFunctions is not null)
        {
            foreach (var synthetic in syntheticFunctions)
            {
                original.Add(synthetic);
            }
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
            _logger.LogTrace("{Type} '{Identifier}' has metadata defined using both the fluent API and [McpMetadata] attributes. Metadata from both sources has been merged.", type, identifier);

            if (overlappingKeys.Count > 0)
            {
                _logger.LogDebug("{Type} '{Identifier}' has overlapping metadata keys: {Keys}. Values from [McpMetadata] attributes will be used.", type, identifier, string.Join(", ", overlappingKeys));
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

    private record ToolPropertyBinding(int Index, JsonObject Binding);

    private void MergeAppUiMetadata(JsonObject jsonObject, string? toolName, ref List<DefaultFunctionMetadata>? syntheticFunctions)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.AppOptions is null)
        {
            return;
        }

        // Build the tool's _meta.ui per the MCP Apps spec (SEP-1865):
        // Only resourceUri and visibility go on the tool metadata.
        // CSP, permissions, border, domain go on the resource response.
        var uiNode = BuildToolUiMetadata(toolName, toolOptions.AppOptions);

        // The binding's "metadata" property becomes `_meta` in the MCP protocol.
        // We need to merge the "ui" key into the existing metadata JSON (if any),
        // or create a new metadata JSON with just the "ui" key.
        JsonObject metaObj;

        if (jsonObject.TryGetPropertyValue("metadata", out var existingMetaNode)
            && existingMetaNode is not null)
        {
            var metaStr = existingMetaNode.GetValue<string>();
            metaObj = JsonNode.Parse(metaStr) as JsonObject ?? new JsonObject();

            if (metaObj.ContainsKey("ui"))
            {
                _logger.LogWarning(
                    "Tool '{ToolName}' defines _meta.ui via McpMetadataAttribute, but the " +
                    "fluent API also configures UI metadata. The fluent API configuration " +
                    "will take precedence.",
                    toolName);
            }
        }
        else
        {
            metaObj = new JsonObject();
        }

        metaObj["ui"] = uiNode;
        jsonObject["metadata"] = metaObj.ToJsonString();

        // Emit synthetic resource function for view serving
        syntheticFunctions ??= [];

        var viewFunction = McpAppFunctionMetadataFactory.CreateViewResourceFunction(toolName);
        syntheticFunctions.Add(viewFunction);
        McpAppUtilities.Register(viewFunction.Name!);
    }

    /// <summary>
    /// Builds the tool's <c>_meta.ui</c> per the MCP Apps spec.
    /// Contains only <c>resourceUri</c> and <c>visibility</c>.
    /// </summary>
    internal static JsonObject BuildToolUiMetadata(string toolName, AppOptions appOptions)
    {
        var ui = new JsonObject
        {
            ["resourceUri"] = McpAppUtilities.ResourceUri(toolName),
            ["visibility"] = SerializeVisibility(appOptions.Visibility)
        };

        return ui;
    }

    internal static JsonArray SerializeVisibility(McpVisibility visibility)
    {
        var array = new JsonArray();
        if (visibility.HasFlag(McpVisibility.Model))
        {
            array.Add("model");
        }

        if (visibility.HasFlag(McpVisibility.App))
        {
            array.Add("app");
        }

        return array;
    }
}
