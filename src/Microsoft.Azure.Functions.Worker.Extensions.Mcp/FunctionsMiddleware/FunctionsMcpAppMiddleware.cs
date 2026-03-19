// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Middleware that intercepts invocations targeting synthetic MCP App resource functions
/// and short-circuits the execution by resolving and returning the view HTML content.
/// Must be registered after <see cref="FunctionsMcpContextMiddleware"/> in the pipeline.
/// </summary>
internal class FunctionsMcpAppMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IOptionsMonitor<ToolOptions> _optionsMonitor;

    public FunctionsMcpAppMiddleware(IOptionsMonitor<ToolOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        var functionName = context.FunctionDefinition.Name;

        if (!McpAppUtilities.IsSyntheticFunction(functionName))
        {
            await next(context);
            return;
        }

        var toolName = McpAppUtilities.ExtractToolName(functionName);
        var toolOptions = _optionsMonitor.Get(toolName);

        if (toolOptions.AppOptions is null)
        {
            await next(context);
            return;
        }

        // Resolve the default view (empty string key)
        if (!toolOptions.AppOptions.Views.TryGetValue(string.Empty, out var view)
            || view.Source is null)
        {
            // Fall back to first available view
            var firstView = toolOptions.AppOptions.Views.Values.FirstOrDefault();
            if (firstView?.Source is null)
            {
                await next(context);
                return;
            }

            view = firstView;
        }

        // Resolve HTML content and short-circuit
        var html = await McpAppFunctions.ResolveViewContentAsync(
            view.Source,
            toolName,
            string.Empty,
            context.CancellationToken);

        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = html;
    }
}
