// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Middleware that intercepts invocations targeting synthetic MCP App resource functions
/// and short-circuits the execution by resolving and returning the view HTML content
/// with the appropriate <c>_meta.ui</c> metadata (CSP, permissions, border, domain).
/// Must be registered after <see cref="FunctionsMcpContextMiddleware"/> in the pipeline.
/// </summary>
internal sealed class FunctionsMcpAppMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IOptionsMonitor<ToolOptions> _optionsMonitor;
    private readonly ILogger<FunctionsMcpAppMiddleware> _logger;

    public FunctionsMcpAppMiddleware(IOptionsMonitor<ToolOptions> optionsMonitor, ILogger<FunctionsMcpAppMiddleware> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
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

        var view = toolOptions.AppOptions.View;
        if (view?.Source is null)
        {
            await next(context);
            return;
        }

        // Resolve HTML content
        string html;
        try
        {
            html = await view.Source.GetContentAsync(context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to resolve view content for MCP App tool '{ToolName}'.", toolName);
            throw;
        }

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
