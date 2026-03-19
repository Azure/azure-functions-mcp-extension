// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
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

        // Resolve HTML content
        var html = await McpAppFunctions.ResolveViewContentAsync(
            view.Source,
            toolName,
            string.Empty,
            context.CancellationToken);

        // Build the resource content JSON with _meta.ui for the resources/read response.
        // The host's ResourceReturnValueBinder will deserialize this as McpResourceResult
        // and pass _meta through to the MCP protocol response.
        var resourceUri = McpAppUtilities.ResourceUri(toolName);
        var resourceContent = BuildResourceContentJson(resourceUri, html, view);

        // Return as McpResourceResult JSON so the host binder picks up _meta
        var resultJson = JsonSerializer.Serialize(new { content = resourceContent });

        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = resultJson;
    }

    /// <summary>
    /// Builds a JSON string representing a ResourceContents object with _meta.ui metadata
    /// for the resources/read response.
    /// </summary>
    private static string BuildResourceContentJson(string uri, string html, ViewOptions viewOptions)
    {
        var content = new JsonObject
        {
            ["uri"] = uri,
            ["mimeType"] = McpAppFunctionMetadataFactory.AppMimeType,
            ["text"] = html,
        };

        // Build _meta.ui with CSP, permissions, border, domain
        var uiMeta = McpAppFunctions.BuildResourceUiMeta(viewOptions);
        if (uiMeta.Count > 0)
        {
            content["_meta"] = new JsonObject { ["ui"] = uiMeta };
        }

        return content.ToJsonString();
    }
}
