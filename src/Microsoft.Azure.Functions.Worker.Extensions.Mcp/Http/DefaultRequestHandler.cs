// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Default request handler that routes between StreamableHttp and SSE handlers
/// </summary>
internal sealed class DefaultRequestHandler(IStreamableHttpRequestHandler streamableHttpRequestHandler, ISseRequestHandler sseRequestHandler) : IMcpRequestHandler
{
    /// <summary>
    /// Handles an incoming MCP request, routing to appropriate handler based on request type
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task HandleRequest(HttpContext context)
    {
        if (sseRequestHandler.IsLegacySseRequest(context))
        {
            await sseRequestHandler.HandleRequest(context);
        }
        else
        {
            await streamableHttpRequestHandler.HandleRequestAsync(context);
        }
    }
}
