// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Handles SSE (Server-Sent Events) requests for MCP in Azure Functions isolated worker process
/// </summary>
internal sealed class SseRequestHandler(
    IMcpInstanceIdProvider instanceIdProvider,
    IMcpClientSessionManager clientSessionManager,
    IOptions<McpOptions> mcpOptions,
    IOptions<McpServerOptions> mcpServerOptions,
    ILoggerFactory loggerFactory) : ISseRequestHandler
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SseRequestHandler>();

    /// <summary>
    /// Determines if the request is a legacy SSE request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>True if this is a legacy SSE request, false otherwise</returns>
    public bool IsLegacySseRequest(HttpContext context)
    {
        var pathSpan = context.Request.Path.Value.AsSpan().TrimEnd('/');

        return pathSpan.EndsWith(SseEndpoint, StringComparison.OrdinalIgnoreCase)
               || pathSpan.EndsWith(MessageEndpoint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Handles an incoming SSE request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task HandleRequest(HttpContext context)
    {
        if (context.Request.Path.Value.AsSpan().TrimEnd('/').EndsWith(SseEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            await HandleSseRequestAsync(context);
        }
        else
        {
            await HandleMessageRequestAsync(context, mcpOptions.Value);
        }
    }

    private async Task HandleSseRequestAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        _logger.LogInformation("Handling legacy SSE request in isolated worker.");

        McpHttpUtility.SetSseContext(context);

        var clientId = Utility.CreateId();
        
        // For Azure Functions isolated worker, we'll provide a minimal SSE implementation
        // In a full implementation, you would create the proper SSE transport here
        await context.Response.WriteAsync("data: {\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n\n");
        await context.Response.Body.FlushAsync();

        // Keep the connection alive while the request is not cancelled
        try
        {
            await context.RequestAborted.WaitHandle.WaitOneAsync(-1, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Normal client disconnect behavior
        }
    }

    private async Task HandleMessageRequestAsync(HttpContext context, McpOptions mcpOptions)
    {
        // Simplified message handling for isolated worker
        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");
    }
}
