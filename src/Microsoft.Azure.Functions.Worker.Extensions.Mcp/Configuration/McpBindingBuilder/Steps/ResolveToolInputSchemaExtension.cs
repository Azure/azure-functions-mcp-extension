// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// For each <c>mcpToolTrigger</c> binding, sets the worker-provided input schema
/// in priority order:
/// <list type="number">
///   <item>Explicit JSON schema configured via <c>McpToolBuilder.WithInputSchema(...)</c>.</item>
///   <item>Schema generated from <c>Context.ResolvedToolProperties</c> (populated
///     earlier from <c>WithProperty(...)</c> or <c>[McpToolProperty]</c>/POCO attribute reflection).</item>
/// </list>
/// When neither applies, the step is a no-op and the host falls back to its own
/// <c>PropertyBasedToolInputSchema</c> (which is also what non-.NET workers rely on).
/// </summary>
internal static class ResolveToolInputSchemaExtension
{
    public static McpBindingBuilder ResolveToolInputSchema(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            var toolName = binding.Identifier!;
            var options = context.ToolOptions.Get(toolName);

            // Priority 1: explicit schema set via WithInputSchema(...)
            if (TryApplyExplicitSchema(binding, options, context.Logger, toolName))
            {
                continue;
            }

            // Priority 2: generate schema from resolved tool properties
            TryApplyPropertyDerivedSchema(binding, context);
        }

        return builder;
    }

    private static bool TryApplyExplicitSchema(McpParsedBinding binding, ToolOptions options, ILogger logger, string toolName)
    {
        var explicitSchema = options.InputSchema;
        if (string.IsNullOrWhiteSpace(explicitSchema))
        {
            return false;
        }

        // The schema was already validated by McpToolBuilder.WithInputSchema.
        // Re-validate defensively in case ToolOptions was mutated directly.
        try
        {
            _ = InputSchemaValidator.ValidateAndParse(explicitSchema, nameof(ToolOptions.InputSchema));
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            logger.LogWarning(ex,
                "Explicit input schema for tool '{ToolName}' is invalid and will be ignored.",
                toolName);
            return false;
        }

        binding.InputSchema = explicitSchema;
        binding.UseWorkerInputSchema = true;
        return true;
    }

    private static void TryApplyPropertyDerivedSchema(McpParsedBinding binding, McpBuilderContext context)
    {
        var props = context.ResolvedToolProperties;
        if (props is null || props.Count == 0)
        {
            return;
        }

        var schema = InputSchemaGenerator.GenerateFromToolProperties(props);
        binding.InputSchema = schema.ToJsonString();
        binding.UseWorkerInputSchema = true;
    }
}

