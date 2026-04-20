// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        ResolveInputSchemas();
        PatchPropertyBindings();

        foreach (var binding in Context.Bindings)
        {
            if (binding.ToolProperties is not null)
            {
                binding.JsonObject["toolProperties"] = binding.ToolProperties;
            }

            if (binding.UseWorkerInputSchema)
            {
                binding.JsonObject["useWorkerInputSchema"] = true;
            }

            if (binding.InputSchema is not null)
            {
                binding.JsonObject["inputSchema"] = binding.InputSchema;
            }

            if (binding.PromptArguments is not null)
            {
                binding.JsonObject["promptArguments"] = binding.PromptArguments;
            }

            if (binding.Metadata is not null)
            {
                binding.JsonObject["metadata"] = binding.Metadata.ToJsonString();
            }

            if (binding.PropertyType is not null)
            {
                binding.JsonObject[McpToolPropertyType] = binding.PropertyType;
            }

            Context.Function.RawBindings![binding.Index] = binding.JsonObject.ToJsonString();
        }
    }

    private void ResolveInputSchemas()
    {
        foreach (var binding in Context.Bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Identifier))
            {
                continue;
            }

            JsonNode? inputSchema = binding.BindingType switch
            {
                McpToolTriggerBindingType => ResolveToolInputSchema(binding.Identifier),
                McpPromptTriggerBindingType => ResolvePromptInputSchema(binding.Identifier),
                McpResourceTriggerBindingType => ResolveResourceInputSchema(binding.Identifier),
                _ => null,
            };

            if (inputSchema is null)
            {
                if (binding.BindingType == McpToolTriggerBindingType)
                {
                    binding.UseWorkerInputSchema = true;

                    Context.Logger.LogWarning(
                        "Failed to generate input schema for tool '{ToolName}' in function '{FunctionName}'. " +
                        "You can provide a custom input schema using the fluent API: " +
                        "builder.ConfigureMcpTool(\"{ToolName2}\").WithInputSchema(...).",
                        binding.Identifier, Context.Function.Name, binding.Identifier);
                }

                continue;
            }

            binding.InputSchema = inputSchema.ToJsonString();
            binding.UseWorkerInputSchema = true;

            if (binding.BindingType == McpToolTriggerBindingType)
            {
                Context.ResolvedInputSchema = inputSchema;
            }
        }
    }

    private JsonNode? ResolveToolInputSchema(string toolName)
    {
        var options = Context.ToolOptions.Get(toolName);

        // Priority 1: Explicit input schema (WithInputSchema)
        if (TryParseExplicitSchema(options.InputSchema, toolName, out var inputSchema))
        {
            return inputSchema;
        }

        // Priority 2: Property-based (from resolved tool properties)
        if (Context.ResolvedToolProperties is not null && Context.ResolvedToolProperties.Count > 0)
        {
            return InputSchemaGenerator.GenerateFromToolProperties(Context.ResolvedToolProperties);
        }

        // Priority 3: Reflection-based
        if (InputSchemaGenerator.TryGenerateFromToolFunction(Context.Function, out inputSchema) && inputSchema is not null)
        {
            return inputSchema;
        }

        return null;
    }

    private JsonNode? ResolvePromptInputSchema(string promptName)
    {
        var options = Context.PromptOptions.Get(promptName);

        // Priority 1: Explicit input schema (WithInputSchema)
        if (TryParseExplicitSchema(options.InputSchema, promptName, out var inputSchema))
        {
            return inputSchema;
        }

        // Priority 2: Argument-based (from resolved prompt arguments)
        if (Context.ResolvedPromptArguments is not null && Context.ResolvedPromptArguments.Count > 0)
        {
            return InputSchemaGenerator.GenerateFromPromptArguments(Context.ResolvedPromptArguments);
        }

        // Priority 3: Reflection-based
        if (InputSchemaGenerator.TryGenerateFromPromptFunction(Context.Function, out inputSchema) && inputSchema is not null)
        {
            return inputSchema;
        }

        return null;
    }

    private JsonNode? ResolveResourceInputSchema(string resourceUri)
    {
        var options = Context.ResourceOptions.Get(resourceUri);

        // Explicit input schema only — resource parameters come from URI templates
        if (TryParseExplicitSchema(options.InputSchema, resourceUri, out var inputSchema))
        {
            return inputSchema;
        }

        return null;
    }

    private bool TryParseExplicitSchema(string? schemaJson, string identifier, out JsonNode? inputSchema)
    {
        inputSchema = null;

        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return false;
        }

        try
        {
            inputSchema = JsonNode.Parse(schemaJson);
            return inputSchema is not null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            Context.Logger.LogWarning(ex,
                "The explicit input schema for '{Identifier}' is not valid JSON. " +
                "Falling back to other schema resolution strategies.",
                identifier);
            return false;
        }
    }

    private void PatchPropertyBindings()
    {
        Dictionary<string, McpParsedBinding>? propertyBindings = null;

        foreach (var binding in Context.Bindings)
        {
            if (binding.BindingType == McpToolPropertyBindingType && binding.Identifier is not null)
            {
                propertyBindings ??= [];
                propertyBindings.TryAdd(binding.Identifier, binding);
            }
        }

        if (propertyBindings is not null && Context.ResolvedInputSchema is not null)
        {
            BindingTypeResolver.ResolveAndApplyTypes(Context.ResolvedInputSchema, propertyBindings);
        }
    }

    private static List<McpParsedBinding> ParseBindings(IFunctionMetadata function)
    {
        var bindings = new List<McpParsedBinding>();

        for (int i = 0; i < function.RawBindings!.Count; i++)
        {
            var node = JsonNode.Parse(function.RawBindings[i]);

            if (node is not JsonObject jsonObject
                || !jsonObject.TryGetPropertyValue("type", out var bindingTypeNode))
            {
                continue;
            }

            var bindingType = bindingTypeNode?.ToString();

            string? identifier = bindingType switch
            {
                McpToolTriggerBindingType => jsonObject["toolName"]?.ToString(),
                McpResourceTriggerBindingType => jsonObject["uri"]?.ToString(),
                McpPromptTriggerBindingType => jsonObject["promptName"]?.ToString(),
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
        try
        {
            if (jsonObject.TryGetPropertyValue("metadata", out var metaNode) && metaNode is not null)
            {
                var metaStr = metaNode.GetValue<string>();
                return JsonNode.Parse(metaStr)?.AsObject();
            }
        }
        catch
        {
            // Malformed metadata is silently ignored; the binding proceeds without it.
        }

        return null;
    }
}
