// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpContextMiddleware : IFunctionsWorkerMiddleware
{
    public FunctionsMcpContextMiddleware() { }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // At the time that this is invoked, nothing has yet hydrated the ToolInvocationContext from the BindingData.
        // Are we supposed to hydrate or call the converter from here? (I don't think this makes sense)
        if (context.BindingContext.BindingData.TryGetValue("mcptoolcontext", out var toolContextValue))
        {
            Console.WriteLine($"toolContextValue found in BindingData. {toolContextValue}");
            context.Items.Add(Constants.ToolInvocationContextKey, toolContextValue!);
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
