// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// HTTP functions for handling MCP requests in Azure Functions isolated worker process
/// </summary>
public static class McpHttpFunctions
{
    /// <summary>
    /// Handles StreamableHttp requests for MCP
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <param name="context">The function context</param>
    /// <returns>The HTTP response</returns>
    [Function("mcp-streamable")]
    public static async Task<HttpResponseData> HandleStreamableRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mcp")] HttpRequestData req,
        FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("HttpContext not available");
            return response;
        }

        var requestHandler = context.InstanceServices.GetService<IMcpRequestHandler>();
        if (requestHandler == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("MCP request handler not configured");
            return response;
        }

        await requestHandler.HandleRequest(httpContext);

        // Return the response that was written to the HttpContext
        var result = req.CreateResponse();
        return result;
    }

    /// <summary>
    /// Handles SSE requests for MCP (legacy support)
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <param name="context">The function context</param>
    /// <returns>The HTTP response</returns>
    [Function("mcp-sse")]
    public static async Task<HttpResponseData> HandleSseRequest(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mcp/sse")] HttpRequestData req,
        FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("HttpContext not available");
            return response;
        }

        var requestHandler = context.InstanceServices.GetService<IMcpRequestHandler>();
        if (requestHandler == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("MCP request handler not configured");
            return response;
        }

        await requestHandler.HandleRequest(httpContext);

        // Return the response that was written to the HttpContext
        var result = req.CreateResponse();
        return result;
    }

    /// <summary>
    /// Handles message requests for MCP (SSE message endpoint)
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <param name="context">The function context</param>
    /// <returns>The HTTP response</returns>
    [Function("mcp-message")]
    public static async Task<HttpResponseData> HandleMessageRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mcp/message")] HttpRequestData req,
        FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("HttpContext not available");
            return response;
        }

        var requestHandler = context.InstanceServices.GetService<IMcpRequestHandler>();
        if (requestHandler == null)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("MCP request handler not configured");
            return response;
        }

        await requestHandler.HandleRequest(httpContext);

        // Return the response that was written to the HttpContext
        var result = req.CreateResponse();
        return result;
    }
}
