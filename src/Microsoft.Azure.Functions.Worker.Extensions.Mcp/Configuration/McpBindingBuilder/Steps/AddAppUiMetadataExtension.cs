// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Merges MCP App UI metadata onto tool trigger bindings and emits synthetic resource functions.
/// </summary>
internal static class AddAppUiMetadataExtension
{
    public static McpBindingBuilder AddAppUiMetadata(this McpBindingBuilder builder, IOptionsMonitor<ToolOptions> toolOptions)
    {
        var context = builder.Context;

        foreach (var binding in context.Bindings)
        {
            if (binding.BindingType != McpToolTriggerBindingType
                || string.IsNullOrWhiteSpace(binding.Identifier))
            {
                continue;
            }

            var options = toolOptions.Get(binding.Identifier);

            if (options.AppOptions is null)
            {
                continue;
            }

            // Build the tool's _meta.ui per the MCP Apps spec (SEP-1865):
            // Only resourceUri and visibility go on the tool metadata.
            // CSP, permissions, border, domain go on the resource response.
            var uiNode = BuildToolUiMetadata(binding.Identifier, options.AppOptions);

            MergeUiIntoMetadata(context, binding, uiNode);

            // Emit synthetic resource function for view serving (once per tool name)
            if (context.EmittedAppTools.Add(binding.Identifier))
            {
                context.SyntheticFunctions.Add(McpAppFunctionMetadataFactory.CreateViewResourceFunction(binding.Identifier));
                context.Logger.LogDebug("Added synthetic MCP App resource function for tool '{ToolName}'.", binding.Identifier);
            }
        }

        return builder;
    }

    private static void MergeUiIntoMetadata(McpBuilderContext context, McpParsedBinding binding, JsonObject uiNode)
    {
        JsonObject metaObj;

        if (binding.JsonObject.TryGetPropertyValue("metadata", out var existingMetaNode)
            && existingMetaNode is not null)
        {
            var metaStr = existingMetaNode is JsonValue jsonValue
                ? jsonValue.GetValue<string>()
                : existingMetaNode.ToJsonString();

            metaObj = JsonNode.Parse(metaStr) as JsonObject ?? new JsonObject();

            if (metaObj.ContainsKey("ui"))
            {
                context.Logger.LogWarning(
                    "Tool '{ToolName}' defines _meta.ui via McpMetadataAttribute, but the " +
                    "fluent API also configures UI metadata. The fluent API configuration " +
                    "will take precedence.",
                    binding.Identifier);
            }
        }
        else
        {
            metaObj = new JsonObject();
        }

        metaObj["ui"] = uiNode;
        binding.JsonObject["metadata"] = metaObj.ToJsonString();
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
