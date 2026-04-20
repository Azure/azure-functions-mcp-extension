// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        if (context.ToolPropertyBindings.Count > 0)
        {
            var propertyBindings = context.ToolPropertyBindings.ToDictionary(b => b.Identifier!, b => b);

            foreach (var property in context.ResolvedToolProperties)
            {
                if (propertyBindings.TryGetValue(property.Name, out var binding))
                {
                    binding.PropertyType = property.Type;
                }
            }
        }

        return builder;
    }
}
