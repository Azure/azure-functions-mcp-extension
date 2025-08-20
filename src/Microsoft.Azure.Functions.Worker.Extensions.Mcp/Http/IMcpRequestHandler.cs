// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Interface for handling MCP requests in Azure Functions isolated worker process
/// </summary>
internal interface IMcpRequestHandler
{
    /// <summary>
    /// Handles an incoming MCP request, routing to appropriate handler based on request type
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleRequest(HttpContext context);
}
