// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Interface for handling SSE (Server-Sent Events) requests in Azure Functions isolated worker process
/// </summary>
internal interface ISseRequestHandler
{
    /// <summary>
    /// Handles an incoming SSE request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleRequest(HttpContext context);

    /// <summary>
    /// Determines if the request is a legacy SSE request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>True if this is a legacy SSE request, false otherwise</returns>
    bool IsLegacySseRequest(HttpContext context);
}
