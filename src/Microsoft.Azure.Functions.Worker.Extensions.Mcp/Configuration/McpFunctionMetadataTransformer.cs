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

        var uiNode = BuildUiMetadata(toolOptions.AppOptions);

        // Parse existing metadata to check for conflicts
        if (jsonObject.TryGetPropertyValue("metadata", out var existingMetaNode)
            && existingMetaNode is not null)
        {
            JsonObject? metaObj = null;
            if (existingMetaNode is JsonValue metaValue)
            {
                var metaStr = metaValue.GetValue<string>();
                metaObj = JsonNode.Parse(metaStr) as JsonObject;
            }
            else if (existingMetaNode is JsonObject existingObj)
            {
                metaObj = existingObj;
            }

            if (metaObj?.TryGetPropertyValue("ui", out var existingUi) == true && existingUi is not null)
            {
                _logger.LogWarning(
                    "Tool '{ToolName}' defines _meta.ui via McpMetadataAttribute, but the " +
                    "fluent API also configures UI metadata. The fluent API configuration " +
                    "will take precedence.",
                    toolName);
            }
        }

        // Emit synthetic HTTP functions for view serving
        syntheticFunctions ??= [];

        var viewFunction = McpAppFunctionMetadataFactory.CreateViewFunction(toolName);
        syntheticFunctions.Add(viewFunction);
        McpAppUtilities.Register(viewFunction.Name!);

        // Emit static assets function if configured
        if (toolOptions.AppOptions.StaticAssetsDirectory is not null)
        {
            var assetsFunction = McpAppFunctionMetadataFactory.CreateStaticAssetsFunction(toolName);
            syntheticFunctions.Add(assetsFunction);
            McpAppUtilities.Register(assetsFunction.Name!);
        }

        jsonObject["appUiMetadata"] = uiNode.ToJsonString();
    }

    internal static JsonObject BuildUiMetadata(AppOptions appOptions)
    {
        var ui = new JsonObject();

        foreach (var (viewName, viewOptions) in appOptions.Views)
        {
            var viewNode = new JsonObject();

            if (viewOptions.Title is not null)
            {
                viewNode["title"] = viewOptions.Title;
            }

            if (viewOptions.Border)
            {
                viewNode["border"] = true;
            }

            if (viewOptions.Domain is not null)
            {
                viewNode["domain"] = viewOptions.Domain;
            }

            if (viewOptions.Csp is not null)
            {
                viewNode["csp"] = BuildCspNode(viewOptions.Csp);
            }

            if (viewOptions.Permissions != McpAppPermissions.None)
            {
                viewNode["permissions"] = SerializePermissions(viewOptions.Permissions);
            }

            // For single unnamed view, properties go directly on the ui object
            // For named views, nest under the view name
            if (string.IsNullOrEmpty(viewName))
            {
                foreach (var prop in viewNode)
                {
                    ui[prop.Key] = prop.Value?.DeepClone();
                }
            }
            else
            {
                ui[viewName] = viewNode;
            }
        }

        ui["visibility"] = SerializeVisibility(appOptions.Visibility);

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

    internal static JsonArray SerializePermissions(McpAppPermissions permissions)
    {
        var array = new JsonArray();
        if (permissions.HasFlag(McpAppPermissions.ClipboardRead))
        {
            array.Add("clipboard-read");
        }

        if (permissions.HasFlag(McpAppPermissions.ClipboardWrite))
        {
            array.Add("clipboard-write");
        }

        return array;
    }

    internal static JsonObject BuildCspNode(CspOptions csp)
    {
        var node = new JsonObject();
        if (csp.ConnectSources.Count > 0)
        {
            node["connect-src"] = new JsonArray(csp.ConnectSources.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.ResourceSources.Count > 0)
        {
            node["default-src"] = new JsonArray(csp.ResourceSources.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.ScriptSources.Count > 0)
        {
            node["script-src"] = new JsonArray(csp.ScriptSources.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.StyleSources.Count > 0)
        {
            node["style-src"] = new JsonArray(csp.StyleSources.Select(s => (JsonNode)s!).ToArray());
        }

        return node;
    }
}
