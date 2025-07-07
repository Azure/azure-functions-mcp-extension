// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Utils.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class StreamableHttpRequestHandler : IStreamableHttpRequestHandler
{
    private static readonly JsonTypeInfo<JsonRpcError> ErrorTypeInfo = (JsonTypeInfo<JsonRpcError>)McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonRpcError));

    public Task HandleRequest(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return HandlePostRequest(context);
        }

        // Temporary
        throw new InvalidOperationException("Unsupported HTTP method. Only POST requests are allowed for streamable requests.");
    }

    private async Task HandlePostRequest(HttpContext context)
    {
        var typedHeaders = context.Request.GetTypedHeaders();

        if (!typedHeaders.Accept.Any(MatchesApplicationJsonMediaType) || !typedHeaders.Accept.Any(MatchesTextEventStreamMediaType))
        {
            await WriteJsonRpcErrorAsync(context,
                "Not Acceptable: Client must accept both application/json and text/event-stream",
                StatusCodes.Status406NotAcceptable);
            return;
        }

        throw new NotImplementedException("Streamable HTTP request handling is not yet implemented.");
    }

    private static Task WriteJsonRpcErrorAsync(HttpContext context, string errorMessage, int statusCode, int errorCode = (int)McpErrorCode.InvalidRequest)
    {
        var jsonRpcError = new JsonRpcError
        {
            Error = new()
            {
                Code = errorCode,
                Message = errorMessage,
            },
        };

        return Results.Json(jsonRpcError, ErrorTypeInfo, statusCode: statusCode).ExecuteAsync(context);
    }

    private static bool MatchesApplicationJsonMediaType(MediaTypeHeaderValue acceptHeaderValue)
        => acceptHeaderValue.MatchesMediaType("application/json");

    private static bool MatchesTextEventStreamMediaType(MediaTypeHeaderValue acceptHeaderValue)
        => acceptHeaderValue.MatchesMediaType("text/event-stream");
}
