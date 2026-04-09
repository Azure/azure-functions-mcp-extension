// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Patches mcpToolProperty bindings with type information from resolved tool properties.
/// </summary>
internal static class PatchPropertyBindingsExtension
{
    public static McpBindingBuilder PatchPropertyBindings(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        if (context.ResolvedToolProperties is null || context.ResolvedToolProperties.Count == 0)
        {
            return builder;
        }

        Dictionary<string, McpParsedBinding>? propertyBindings = null;

        foreach (var binding in context.Bindings)
        {
            if (binding.BindingType == McpToolPropertyBindingType && binding.Identifier is not null)
            {
                propertyBindings ??= [];
                propertyBindings.TryAdd(binding.Identifier, binding);
            }
        }

        if (propertyBindings is not null)
        {
            foreach (var property in context.ResolvedToolProperties)
            {
                if (propertyBindings.TryGetValue(property.Name, out var binding))
                {
                    binding.JsonObject[McpToolPropertyType] = property.Type;
                }
            }
        }

        return builder;
    }
}
