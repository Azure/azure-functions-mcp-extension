// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Patches mcpToolProperty bindings with type information from the resolved input schema.
/// </summary>
internal static class PatchPropertyBindingsExtension
{
    public static McpBindingBuilder PatchPropertyBindings(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        if (context.ResolvedInputSchema is null)
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
            BindingTypeResolver.ResolveAndApplyTypes(context.ResolvedInputSchema, propertyBindings);
        }

        return builder;
    }
}
