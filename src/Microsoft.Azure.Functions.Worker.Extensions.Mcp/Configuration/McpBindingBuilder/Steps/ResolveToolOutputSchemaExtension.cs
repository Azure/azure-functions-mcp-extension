// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// For each <c>mcpToolTrigger</c> binding, applies an explicit output schema configured
/// via <c>McpToolBuilder.WithOutputSchema(...)</c>.
///
/// <para>
/// When no explicit schema is configured, this step is a no-op and the tool will not
/// advertise an output schema.
/// </para>
/// </summary>
internal static class ResolveToolOutputSchemaExtension
{
    public static McpBindingBuilder ResolveToolOutputSchema(this McpBindingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var context = builder.Context;

        foreach (var binding in context.ToolTriggerBindings)
        {
            var toolName = binding.Identifier!;
            var options = context.ToolOptions.Get(toolName);

            if (options.OutputSchema is null)
            {
                continue;
            }

            binding.OutputSchema = options.OutputSchema.Json;
        }

        return builder;
    }
}