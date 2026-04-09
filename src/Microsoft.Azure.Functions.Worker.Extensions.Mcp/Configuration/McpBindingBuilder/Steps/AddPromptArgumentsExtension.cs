// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Resolves prompt arguments for prompt trigger bindings.
/// </summary>
internal static class AddPromptArgumentsExtension
{
    public static McpBindingBuilder AddPromptArguments(this McpBindingBuilder builder, IOptionsMonitor<PromptOptions> promptOptions)
    {
        var context = builder.Context;

        foreach (var binding in context.Bindings)
        {
            if (binding.BindingType != McpPromptTriggerBindingType
                || string.IsNullOrWhiteSpace(binding.Identifier))
            {
                continue;
            }

            if (TryResolvePromptArguments(context, binding.Identifier, promptOptions, out var promptArguments))
            {
                binding.JsonObject["promptArguments"] = PromptArgumentParser.GetArgumentsJson(promptArguments);
                context.ResolvedPromptArguments = promptArguments;
            }
        }

        return builder;
    }

    private static bool TryResolvePromptArguments(
        McpBuilderContext context, string promptName, IOptionsMonitor<PromptOptions> promptOptions, out List<PromptArgumentDefinition> promptArguments)
    {
        // Priority 1: Configured options via AddArgument() fluent API
        var options = promptOptions.Get(promptName);
        if (options.Arguments.Count != 0)
        {
            promptArguments = options.Arguments;
            return true;
        }

        // Priority 2: Attribute-based reflection
        if (PromptArgumentParser.TryGetArgumentsFromAttributes(context.Function, out var attributeArguments)
            && attributeArguments is not null)
        {
            promptArguments = attributeArguments;
            return true;
        }

        promptArguments = [];
        return false;
    }
}
