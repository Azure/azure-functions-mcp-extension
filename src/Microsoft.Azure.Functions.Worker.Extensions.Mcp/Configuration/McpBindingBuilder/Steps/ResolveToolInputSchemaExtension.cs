// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// For each <c>mcpToolTrigger</c> binding, applies an explicit input schema configured
/// via <c>McpToolBuilder.WithInputSchema(...)</c> and signals the host to use it
/// (<c>useWorkerInputSchema = true</c>).
///
/// <para>
/// When no explicit schema is configured, this step is a no-op and the host continues
/// to generate a schema from <c>toolProperties</c> as before.
/// </para>
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

            var explicitSchema = options.InputSchema;
            if (string.IsNullOrWhiteSpace(explicitSchema))
            {
                continue;
            }

            // The schema was already validated by McpToolBuilder.WithInputSchema.
            // Re-validate defensively in case ToolOptions was mutated directly.
            try
            {
                _ = InputSchemaValidator.ValidateAndParse(explicitSchema, nameof(ToolOptions.InputSchema));
            }
            catch (Exception ex) when (ex is ArgumentException or JsonException)
            {
                context.Logger.LogWarning(ex,
                    "Explicit input schema for tool '{ToolName}' is invalid and will be ignored.",
                    toolName);
                continue;
            }

            binding.InputSchema = explicitSchema;
            binding.UseWorkerInputSchema = true;
        }

        return builder;
    }
}
