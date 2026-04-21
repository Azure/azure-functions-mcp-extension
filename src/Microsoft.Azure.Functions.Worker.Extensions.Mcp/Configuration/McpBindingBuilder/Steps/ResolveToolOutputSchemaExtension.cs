// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// For each <c>mcpToolTrigger</c> binding, resolves the output schema using a priority-based strategy:
/// <list type="number">
///   <item>Explicit output schema configured via <c>McpToolBuilder.WithOutputSchema(...)</c>.</item>
///   <item>Reflection-based output schema auto-generated from a return type decorated with <see cref="McpOutputAttribute"/>.</item>
/// </list>
///
/// <para>
/// When no output schema is resolved, this step is a no-op and the tool will not advertise an output schema.
/// </para>
/// </summary>
internal static class ResolveToolOutputSchemaExtension
{
    public static McpBindingBuilder ResolveToolOutputSchema(this McpBindingBuilder builder)
    {
        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            var toolName = binding.Identifier!;
            var options = context.ToolOptions.Get(toolName);

            // Priority 1: Explicit output schema (WithOutputSchema)
            var explicitSchema = options.OutputSchema;
            if (!string.IsNullOrWhiteSpace(explicitSchema))
            {
                try
                {
                    var parsed = JsonNode.Parse(explicitSchema);
                    if (parsed is not null)
                    {
                        binding.OutputSchema = explicitSchema;
                        continue;
                    }
                }
                catch (JsonException ex)
                {
                    context.Logger.LogWarning(ex,
                        "Explicit output schema for tool '{ToolName}' is not valid JSON and will be ignored.",
                        toolName);
                }
            }

            // Priority 2: Reflection-based (auto-generated from [McpOutput] on return type)
            if (OutputSchemaGenerator.TryGenerateFromFunction(context.Function, out var outputSchema, context.Logger)
                && outputSchema is not null)
            {
                binding.OutputSchema = outputSchema.ToJsonString();
            }
        }

        return builder;
    }
}
