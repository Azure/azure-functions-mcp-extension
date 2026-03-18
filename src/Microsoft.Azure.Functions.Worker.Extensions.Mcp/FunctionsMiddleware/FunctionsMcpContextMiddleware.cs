// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpContextMiddleware(IOptionsMonitor<ToolOptions> toolOptionsMonitor) : IFunctionsWorkerMiddleware
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Short-circuit synthetic MCP App functions — serve HTML directly without invoking user code.
        if (context.FunctionDefinition.Name.StartsWith(Constants.SyntheticFunctionPrefix, StringComparison.Ordinal))
        {
            await HandleSyntheticAppFunction(context);
            return;
        }

        // Get the tool invocation context via the name of the trigger binding
        TryAddInvocationContext(
            context,
            (out string? name) => context.TryGetMcpToolTriggerName(out name),
            Constants.ToolInvocationContextKey,
            McpJsonContext.Default.ToolInvocationContext);

        // Get the resource invocation context via the name of the trigger binding
        TryAddInvocationContext(
            context,
            (out string? name) => context.TryGetMcpResourceTriggerName(out name),
            Constants.ResourceInvocationContextKey,
            McpJsonContext.Default.ResourceInvocationContext);

        await next(context);
    }

    private async Task HandleSyntheticAppFunction(FunctionContext context)
    {
        var functionName = context.FunctionDefinition.Name;

        // Extract tool name: __McpApp_{toolName}_View
        var toolName = functionName
            .Substring(Constants.SyntheticFunctionPrefix.Length,
                functionName.Length - Constants.SyntheticFunctionPrefix.Length - Constants.SyntheticFunctionSuffix.Length);

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.AppOptions?.ViewFilePath is not { } viewFilePath)
        {
            throw new InvalidOperationException(
                $"Synthetic MCP App function '{functionName}' has no ViewFilePath configured.");
        }

        // Resolve relative to the function app's root directory
        var appDir = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
                     ?? AppContext.BaseDirectory;
        var fullPath = Path.Combine(appDir, viewFilePath);

        var html = await File.ReadAllTextAsync(fullPath);
        context.GetInvocationResult().Value = html;
    }

    private delegate bool TryGetTriggerNameDelegate(out string? triggerName);

    private static void TryAddInvocationContext<T>(
        FunctionContext context,
        TryGetTriggerNameDelegate tryGetTriggerName,
        string contextKey,
        JsonTypeInfo<T> jsonTypeInfo) where T : class
    {
        if (tryGetTriggerName(out string? triggerName)
            && !string.IsNullOrEmpty(triggerName)
            && context.BindingContext.BindingData.TryGetValue(triggerName, out var mcpContext))
        {
            T? invocationContext = JsonSerializer.Deserialize(mcpContext?.ToString()!, jsonTypeInfo);
            context.Items.Add(contextKey, invocationContext!);
        }
    }
}
