// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Applies fluent API and attributed metadata onto trigger bindings.
/// </summary>
internal static class AddMetadataExtension
{
    public static McpBindingBuilder AddMetadata(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            ApplyFluentMetadata(binding, context.ToolOptions);
            if (MetadataParser.TryGetToolMetadata(context.Function, out var toolMetadataJson))
            {
                ApplyOrMergeAttributedMetadata(context, binding, toolMetadataJson, "Tool");
            }
        }

        foreach (var binding in context.ResourceTriggerBindings)
        {
            ApplyFluentMetadata(binding, context.ResourceOptions);
            if (MetadataParser.TryGetResourceMetadata(context.Function, out var resourceMetadataJson))
            {
                ApplyOrMergeAttributedMetadata(context, binding, resourceMetadataJson, "Resource");
            }
        }

        foreach (var binding in context.PromptTriggerBindings)
        {
            ApplyFluentMetadata(binding, context.PromptOptions);
            if (MetadataParser.TryGetPromptMetadata(context.Function, out var promptMetadataJson))
            {
                ApplyOrMergeAttributedMetadata(context, binding, promptMetadataJson, "Prompt");
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
            binding.Metadata ??= new JsonObject();
            foreach (var kvp in options.Metadata)
            {
                binding.Metadata[kvp.Key] = JsonValue.Create(kvp.Value);
            }
        }
    }

    private static void ApplyOrMergeAttributedMetadata(McpBuilderContext context, McpParsedBinding binding, string attributedMetadataJson, string type)
    {
        if (binding.Metadata is not null)
        {
            var fluentJson = binding.Metadata.ToJsonString();
            var merged = MetadataMerger.MergeMetadata(fluentJson, attributedMetadataJson, out var overlappingKeys);
            binding.Metadata = JsonNode.Parse(merged)?.AsObject() ?? new JsonObject();

            context.Logger.LogTrace("{Type} '{Identifier}' has metadata defined using both the fluent API and [McpMetadata] attributes. Metadata from both sources has been merged.", type, binding.Identifier);

            if (overlappingKeys.Count > 0)
            {
                context.Logger.LogDebug("{Type} '{Identifier}' has overlapping metadata keys: {Keys}. Values from [McpMetadata] attributes will be used.", type, binding.Identifier, string.Join(", ", overlappingKeys));
            }
        }
        else
        {
            binding.Metadata = JsonNode.Parse(attributedMetadataJson)?.AsObject() ?? new JsonObject();
        }
    }
}
