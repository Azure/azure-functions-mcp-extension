// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        ArgumentNullException.ThrowIfNull(builder);

        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            var toolName = binding.Identifier!;
            var options = context.ToolOptions.Get(toolName);

            if (options.InputSchema is null)
            {
                continue;
            }

            binding.InputSchema = options.InputSchema.Json;
            binding.UseWorkerInputSchema = true;
        }

        return builder;
    }
}
