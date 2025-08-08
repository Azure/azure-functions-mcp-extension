// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpContextMiddleware : IFunctionsWorkerMiddleware
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public FunctionsMcpContextMiddleware() { }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Get the tool invocation context via the name of the trigger binding
        if (context.TryGetMcpToolTriggerName(out string triggerName)
            && context.BindingContext.BindingData.TryGetValue(triggerName, out var mcpToolContext))
        {
            var toolInvocationContext = JsonSerializer.Deserialize<ToolInvocationContext>(mcpToolContext?.ToString()!, _serializerOptions);
            context.Items.Add(Constants.ToolInvocationContextKey, toolInvocationContext!);
        }

        try
        {
            await next(context);
        }
        finally
        {
        }
    }
}
