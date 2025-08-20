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
            var toolInvocationContext = JsonSerializer.Deserialize(mcpToolContext?.ToString()!, McpJsonContext.Default.ToolInvocationContext);
            context.Items.Add(Constants.ToolInvocationContextKey, toolInvocationContext!);
        }

        await next(context);
    }
}
