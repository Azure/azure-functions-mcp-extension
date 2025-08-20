// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Interface for handling StreamableHttp requests in Azure Functions isolated worker process
/// </summary>
internal interface IStreamableHttpRequestHandler
{
    /// <summary>
    /// Handles an incoming StreamableHttp request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleRequestAsync(HttpContext context);
}
