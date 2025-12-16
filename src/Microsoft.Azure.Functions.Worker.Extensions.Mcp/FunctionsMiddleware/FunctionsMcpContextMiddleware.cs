// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpContextMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Get the tool invocation context via the name of the trigger binding
        if (context.TryGetMcpToolTriggerName(out string? triggerName)
            && context.BindingContext.BindingData.TryGetValue(triggerName, out var mcpToolContext))
        {
            ToolInvocationContext? toolInvocationContext = JsonSerializer.Deserialize(mcpToolContext?.ToString()!, McpJsonContext.Default.ToolInvocationContext);

            context.Items.Add(Constants.ToolInvocationContextKey, toolInvocationContext!);
        }

        // Get the resource invocation context via the name of the trigger binding
        if (context.TryGetMcpResourceTriggerName(out string? resourceTriggerName)
            && context.BindingContext.BindingData.TryGetValue(resourceTriggerName, out var mcpResourceContext))
        {
            ResourceInvocationContext? resourceInvocationContext = JsonSerializer.Deserialize(mcpResourceContext?.ToString()!, McpJsonContext.Default.ResourceInvocationContext);

            context.Items.Add(Constants.ResourceInvocationContextKey, resourceInvocationContext!);
        }

        await next(context);
    }
}
