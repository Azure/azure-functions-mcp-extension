// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Resolves tool properties for tool trigger bindings.
/// </summary>
internal static class AddToolPropertiesExtension
{
    public static McpBindingBuilder AddToolProperties(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            if (TryResolveToolProperties(context, binding.Identifier!, context.ToolOptions, out var toolProperties))
            {
                binding.ToolProperties = ToolPropertyParser.GetPropertiesJson(toolProperties);
                context.ResolvedToolProperties = toolProperties;
            }
        }

        return builder;
    }

    private static bool TryResolveToolProperties(
        McpBuilderContext context, string toolName, IOptionsMonitor<ToolOptions> toolOptions, out List<ToolProperty> toolProperties)
    {
        // Priority 1: Configured options via WithProperty() fluent API
        var options = toolOptions.Get(toolName);
        if (options.Properties.Count != 0)
        {
            toolProperties = options.Properties;
            return true;
        }

        // Priority 2: Attribute-based reflection
        if (ToolPropertyParser.TryGetPropertiesFromAttributes(context.Function, out var attributeProperties)
            && attributeProperties is not null)
        {
            toolProperties = attributeProperties;
            return true;
        }

        toolProperties = [];
        return false;
    }
}
