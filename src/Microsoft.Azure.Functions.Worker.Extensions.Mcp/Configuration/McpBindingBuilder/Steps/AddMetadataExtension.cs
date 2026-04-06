// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Applies fluent API and attributed metadata onto trigger bindings.
/// </summary>
internal static class AddMetadataExtension
{
    public static McpBindingBuilder AddMetadata(
        this McpBindingBuilder builder,
        IOptionsMonitor<ToolOptions> toolOptions,
        IOptionsMonitor<ResourceOptions> resourceOptions,
        IOptionsMonitor<PromptOptions> promptOptions)
    {
        var context = builder.Context;

        foreach (var binding in context.Bindings)
        {
            switch (binding.BindingType)
            {
                case McpToolTriggerBindingType:
                    ApplyFluentMetadata(binding, toolOptions);
                    if (MetadataParser.TryGetToolMetadata(context.Function, out var toolMetadataJson))
                    {
                        ApplyOrMergeAttributedMetadata(context, binding, toolMetadataJson, "Tool");
                    }
                    break;

                case McpResourceTriggerBindingType:
                    ApplyFluentMetadata(binding, resourceOptions);
                    if (MetadataParser.TryGetResourceMetadata(context.Function, out var resourceMetadataJson))
                    {
                        ApplyOrMergeAttributedMetadata(context, binding, resourceMetadataJson, "Resource");
                    }
                    break;

                case McpPromptTriggerBindingType:
                    ApplyFluentMetadata(binding, promptOptions);
                    if (MetadataParser.TryGetPromptMetadata(context.Function, out var promptMetadataJson))
                    {
                        ApplyOrMergeAttributedMetadata(context, binding, promptMetadataJson, "Prompt");
                    }
                    break;
            }
        }

        return builder;
    }

    private static void ApplyFluentMetadata<TOptions>(McpParsedBinding binding, IOptionsMonitor<TOptions> optionsMonitor)
        where TOptions : McpBuilderOptions
    {
        if (string.IsNullOrWhiteSpace(binding.Identifier))
        {
            return;
        }

        var options = optionsMonitor.Get(binding.Identifier);

        if (options.Metadata.Count > 0)
        {
            binding.JsonObject["metadata"] = JsonSerializer.Serialize(options.Metadata);
        }
    }

    private static void ApplyOrMergeAttributedMetadata(McpBuilderContext context, McpParsedBinding binding, string attributedMetadataJson, string type)
    {
        if (binding.JsonObject.ContainsKey("metadata"))
        {
            binding.JsonObject["metadata"] = MetadataMerger.MergeMetadata(
                binding.JsonObject["metadata"]?.GetValue<string>(), attributedMetadataJson, out var overlappingKeys);

            context.Logger.LogTrace("{Type} '{Identifier}' has metadata defined using both the fluent API and [McpMetadata] attributes. Metadata from both sources has been merged.", type, binding.Identifier);

            if (overlappingKeys.Count > 0)
            {
                context.Logger.LogDebug("{Type} '{Identifier}' has overlapping metadata keys: {Keys}. Values from [McpMetadata] attributes will be used.", type, binding.Identifier, string.Join(", ", overlappingKeys));
            }
        }
        else
        {
            binding.JsonObject["metadata"] = attributedMetadataJson;
        }
    }
}
