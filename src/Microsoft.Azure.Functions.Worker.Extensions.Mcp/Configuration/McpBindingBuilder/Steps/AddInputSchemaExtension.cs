// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;

/// <summary>
/// Resolves input schema for tool trigger bindings using a priority-based strategy:
/// 1. Explicit input schema (set via <c>WithInputSchema(...)</c> fluent API)
/// 2. Property-based input schema (generated from <c>WithProperty(...)</c> fluent API)
/// 3. Reflection-based input schema (generated from function method parameters)
/// </summary>
internal static class AddInputSchemaExtension
{
    public static McpBindingBuilder AddInputSchema(this McpBindingBuilder builder, IOptionsMonitor<ToolOptions> toolOptions)
    {
        var context = builder.Context;

        foreach (var binding in context.Bindings)
        {
            if (binding.BindingType != McpToolTriggerBindingType
                || string.IsNullOrWhiteSpace(binding.Identifier))
            {
                continue;
            }

            binding.JsonObject["useWorkerInputSchema"] = true;

            if (TryResolveInputSchema(binding.Identifier, context.Function, toolOptions, out var inputSchema))
            {
                binding.JsonObject["inputSchema"] = inputSchema!.ToJsonString();
                context.ResolvedInputSchema = inputSchema;
            }
            else
            {
                context.Logger.LogWarning(
                    "Failed to generate input schema for tool '{ToolName}' in function '{FunctionName}'. " +
                    "You can provide a custom input schema using the fluent API: " +
                    "builder.ConfigureMcpTool(\"{ToolName}\").WithInputSchema(...).",
                    binding.Identifier, context.Function.Name, binding.Identifier);
            }
        }

        return builder;
    }

    private static bool TryResolveInputSchema(
        string toolName,
        IFunctionMetadata function,
        IOptionsMonitor<ToolOptions> toolOptions,
        out JsonNode? inputSchema)
    {
        inputSchema = null;

        var options = toolOptions.Get(toolName);

        // Priority 1: Explicit input schema (WithInputSchema)
        if (!string.IsNullOrWhiteSpace(options.InputSchema))
        {
            inputSchema = JsonNode.Parse(options.InputSchema);
            return inputSchema is not null;
        }

        // Priority 2: Property-based (WithProperty)
        if (options.Properties.Count > 0)
        {
            inputSchema = InputSchemaGenerator.GenerateFromToolProperties(options.Properties);
            return true;
        }

        // Priority 3: Reflection-based
        if (InputSchemaGenerator.TryGenerateFromFunction(function, out inputSchema) && inputSchema is not null)
        {
            return true;
        }

        inputSchema = null;
        return false;
    }
}
