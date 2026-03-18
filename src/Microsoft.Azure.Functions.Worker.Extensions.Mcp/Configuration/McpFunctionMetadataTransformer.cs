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
        List<(string ToolName, AppOptions App)>? appToolsToEmit = null;

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
                        var currentToolName = jsonObject["toolName"]?.ToString();

                        if (currentToolName is not null
                            && GetToolProperties(currentToolName, function, out toolProperties))
                        {
                            jsonObject["toolProperties"] = ToolPropertyParser.GetPropertiesJson(toolProperties);
                        }

                        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson))
                        {
                            jsonObject["metadata"] = toolMetadataJson;
                        }

                        // Inject _meta.ui for MCP App tools
                        if (currentToolName is not null)
                        {
                            var toolOptions = toolOptionsMonitor.Get(currentToolName);
                            if (toolOptions.AppOptions is { } appOptions)
                            {
                                InjectAppMetadata(jsonObject, appOptions);

                                appToolsToEmit ??= [];
                                appToolsToEmit.Add((currentToolName, appOptions));
                            }
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

            // This is required for attributed properties/input bindings:
            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties);
        }

        // Emit synthetic resource functions for MCP App tools
        if (appToolsToEmit is not null)
        {
            foreach (var (toolName, appOptions) in appToolsToEmit)
            {
                var syntheticName = $"{SyntheticFunctionPrefix}{toolName}{SyntheticFunctionSuffix}";

                var resourceBinding = new JsonObject
                {
                    ["type"] = McpResourceTriggerBindingType,
                    ["direction"] = "In",
                    ["name"] = "context",
                    ["title"] = $"{toolName} UI",
                    ["uri"] = appOptions.ResourceUri,
                    ["resourceName"] = $"{toolName}_ui",
                    ["mimeType"] = McpAppMimeType
                };

                original.Add(new SyntheticFunctionMetadata(syntheticName, resourceBinding.ToJsonString()));
            }
        }
    }

    private static void InjectAppMetadata(JsonObject jsonObject, AppOptions appOptions)
    {
        // Parse any existing metadata
        var existingMetadataJson = jsonObject["metadata"]?.GetValue<string>();
        JsonObject metadataObj;

        if (!string.IsNullOrEmpty(existingMetadataJson))
        {
            metadataObj = JsonNode.Parse(existingMetadataJson)?.AsObject() ?? new JsonObject();
        }
        else
        {
            metadataObj = new JsonObject();
        }

        // Build the ui object (placed directly in metadata, which maps to _meta in the protocol)
        var uiNode = new JsonObject
        {
            ["resourceUri"] = appOptions.ResourceUri,
            ["visibility"] = SerializeVisibility(appOptions.Visibility)
        };

        if (appOptions.PrefersBorder.HasValue)
        {
            uiNode["prefersBorder"] = appOptions.PrefersBorder.Value;
        }

        if (appOptions.Domain is not null)
        {
            uiNode["domain"] = appOptions.Domain;
        }

        if (appOptions.Csp is not null)
        {
            uiNode["csp"] = SerializeCsp(appOptions.Csp);
        }

        if (appOptions.Permissions != McpAppPermissions.None)
        {
            uiNode["permissions"] = SerializePermissions(appOptions.Permissions);
        }

        // The metadata dictionary is serialized directly into Tool.Meta (_meta in the wire protocol),
        // so "ui" should be a direct child of metadataObj, not nested under another "_meta" key.
        metadataObj["ui"] = uiNode;

        jsonObject["metadata"] = metadataObj.ToJsonString();
    }

    private static JsonNode SerializeVisibility(McpVisibility visibility)
    {
        return visibility switch
        {
            McpVisibility.Model => new JsonArray("model"),
            McpVisibility.App => new JsonArray("app"),
            McpVisibility.ModelAndApp => new JsonArray("model", "app"),
            _ => new JsonArray("model", "app")
        };
    }

    private static JsonObject SerializeCsp(AppCspOptions csp)
    {
        var cspNode = new JsonObject();

        if (csp.ConnectDomains is { Count: > 0 })
        {
            var arr = new JsonArray();
            foreach (var d in csp.ConnectDomains) arr.Add(d);
            cspNode["connectDomains"] = arr;
        }

        if (csp.ResourceDomains is { Count: > 0 })
        {
            var arr = new JsonArray();
            foreach (var d in csp.ResourceDomains) arr.Add(d);
            cspNode["resourceDomains"] = arr;
        }

        return cspNode;
    }

    private static JsonObject SerializePermissions(McpAppPermissions permissions)
    {
        var obj = new JsonObject();

        if (permissions.HasFlag(McpAppPermissions.ClipboardWrite))
        {
            obj["clipboardWrite"] = new JsonObject();
        }

        if (permissions.HasFlag(McpAppPermissions.ClipboardRead))
        {
            obj["clipboardRead"] = new JsonObject();
        }

        return obj;
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

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
