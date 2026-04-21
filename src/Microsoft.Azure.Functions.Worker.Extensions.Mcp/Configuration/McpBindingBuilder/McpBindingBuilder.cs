// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Fluent builder that enriches a function's MCP bindings step by step.
/// Add new transformation methods via extension methods in separate files —
/// the builder itself never needs to change.
/// </summary>
internal sealed class McpBindingBuilder
{
    public McpBindingBuilder(
        IFunctionMetadata function,
        ILogger logger,
        IOptionsMonitor<ToolOptions> toolOptions,
        IOptionsMonitor<ResourceOptions> resourceOptions,
        IOptionsMonitor<PromptOptions> promptOptions,
        HashSet<string>? sharedEmittedAppTools = null)
    {
        var bindings = ParseBindings(function);
        Context = new McpBuilderContext(function, bindings, logger, toolOptions, resourceOptions, promptOptions, sharedEmittedAppTools);
    }

    internal McpBuilderContext Context { get; }

    public bool HasBindings => Context.Bindings.Count > 0;

    public void Build()
    {
        foreach (var binding in Context.Bindings)
        {
            if (binding.ToolProperties is not null)
            {
                binding.JsonObject[McpToolProperties] = binding.ToolProperties;
            }

            if (binding.PromptArguments is not null)
            {
                binding.JsonObject[McpPromptArguments] = binding.PromptArguments;
            }

            if (binding.Metadata is not null)
            {
                binding.JsonObject[McpMetadata] = binding.Metadata.ToJsonString();
            }

            if (binding.PropertyType is not null)
            {
                binding.JsonObject[McpToolPropertyType] = binding.PropertyType;
            }

            Context.Function.RawBindings![binding.Index] = binding.JsonObject.ToJsonString();
        }
    }

    private static List<McpParsedBinding> ParseBindings(IFunctionMetadata function)
    {
        var bindings = new List<McpParsedBinding>();

        for (int i = 0; i < function.RawBindings!.Count; i++)
        {
            var node = JsonNode.Parse(function.RawBindings[i]);

            if (node is not JsonObject jsonObject
                || !jsonObject.TryGetPropertyValue(BindingType, out var bindingTypeNode))
            {
                continue;
            }

            var bindingType = bindingTypeNode?.ToString();

            string? identifier = bindingType switch
            {
                McpToolTriggerBindingType => jsonObject[McpToolName]?.ToString(),
                McpResourceTriggerBindingType => jsonObject[McpUri]?.ToString(),
                McpPromptTriggerBindingType => jsonObject[McpPromptName]?.ToString(),
                McpToolPropertyBindingType => jsonObject[McpToolPropertyName]?.ToString(),
                McpPromptArgumentBindingType => jsonObject[McpPromptArgumentName]?.ToString(),
                _ => null,
            };

            if (identifier is null)
            {
                continue;
            }

            bindings.Add(new McpParsedBinding(i, bindingType!, identifier, jsonObject)
            {
                Metadata = TryParseExistingMetadata(jsonObject)
            });
        }

        return bindings;
    }

    /// <summary>
    /// Extracts any pre-existing metadata from the raw binding JSON into a JsonObject
    /// so that steps can work with the structured model rather than the serialized string.
    /// </summary>
    private static JsonObject? TryParseExistingMetadata(JsonObject jsonObject)
    {
        if (!jsonObject.TryGetPropertyValue(McpMetadata, out var metaNode) || metaNode is null)
        {
            return null;
        }

        try
        {
            var metaStr = metaNode is JsonValue jv
                ? jv.GetValue<string>()
                : metaNode.ToJsonString();

            return JsonNode.Parse(metaStr)?.AsObject();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Malformed metadata is silently ignored; the binding proceeds without it.
            return null;
        }
    }
}
