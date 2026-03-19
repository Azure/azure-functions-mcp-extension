// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Middleware that intercepts invocations targeting synthetic MCP App resource functions
/// and short-circuits the execution by resolving and returning the view HTML content
/// with the appropriate <c>_meta.ui</c> metadata (CSP, permissions, border, domain).
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


        if (!context.Items.ContainsKey(Constants.ResourceInvocationContextKey)
            || !McpAppUtilities.IsSyntheticFunction(context.FunctionDefinition.Name))
        {
            await next(context);
            return;
        }

        var toolName = McpAppUtilities.ExtractToolName(context.FunctionDefinition.Name);
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

        // Resolve HTML content
        var html = await view.Source.GetContentAsync(context.CancellationToken);

        // Build the resource content with _meta.ui for the resources/read response.
        var resourceContent = new McpAppResourceContent
        {
            Uri = McpAppUtilities.ResourceUri(toolName),
            MimeType = McpAppFunctionMetadataFactory.AppMimeType,
            Text = html,
            Meta = BuildResourceMeta(view),
        };

        // Serialize as McpResourceResult so the host's ResourceReturnValueBinder
        // picks up the full content including _meta.
        var contentJson = JsonSerializer.Serialize(resourceContent, McpJsonContext.Default.McpAppResourceContent);
        var result = new McpAppResourceResult { Content = contentJson };
        var resultJson = JsonSerializer.Serialize(result, McpJsonContext.Default.McpAppResourceResult);

        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = resultJson;
    }

    private static McpAppResourceMeta? BuildResourceMeta(ViewOptions viewOptions)
    {
        var uiMeta = McpAppFunctions.BuildResourceUiMeta(viewOptions);
        if (uiMeta is null)
        {
            return null;
        }

        return new McpAppResourceMeta { Ui = uiMeta };
    }
}
