// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Middleware;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpContextMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

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
