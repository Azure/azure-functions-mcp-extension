// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using static Microsoft.Azure.Functions.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Http;

internal sealed class McpHttpUtility
{
    internal static bool TryGetFunctionKey(HttpContext context, out string? code)
    {
        return TryGetQueryValue(context, FunctionsCodeQuery, out code) ||
               context.Request.Headers.TryGetValue(FunctionsKeyHeader, out StringValues values) &&
               (code = values.FirstOrDefault()) is not null;
    }

    internal static bool TryGetQueryValue(HttpContext context, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (context.Request.Query.TryGetValue(key, out var strings))
        {
            value = strings.FirstOrDefault();
        }

        return value is not null;
    }

    internal static void SetSseContext(HttpContext context)
    {
        // Set the appropriate headers for SSE.
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache,no-store";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers.ContentEncoding = "identity";

        context.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();
    }

    internal static async ValueTask<JsonRpcMessage?> ProcessJsonRpcPayloadAsync(HttpRequest request, JsonSerializerOptions options, CancellationToken cancellationToken, bool unwrapOnly = false)
    {
        // Process the incoming request body as JSON. Support both raw JSON-RPC messages and
        // wrapped payloads with the shape: { "isFunctionsMcpResult": true, "content": <JSON-RPC message> }
        // 
        // When unwrapOnly is false: If the wrapper is present, deserialize the inner "content" as the JsonRpcMessage. 
        // Otherwise, deserialize the root object directly as a JsonRpcMessage and return it.
        //
        // When unwrapOnly is true: If the wrapper is present, replace the request.Body stream with a memory stream 
        // containing only the inner content. If no wrapper is present, leave the original body intact.

        // If the body is empty, return null.
        if (request.ContentLength == null || request.ContentLength == 0)
        {
            return null;
        }

        // Read the request body into a JsonDocument for inspection.
        request.EnableBuffering();
        try
        {
            using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            JsonElement messageElement = root;
            bool isWrapped = false;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("isFunctionsMcpResult", out var marker) &&
                marker.ValueKind == JsonValueKind.True &&
                root.TryGetProperty("content", out var content))
            {
                messageElement = content;
                isWrapped = true;
            }

            if (unwrapOnly)
            {
                if (isWrapped)
                {
                    var inner = messageElement.GetRawText();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(inner);
                    request.Body = new MemoryStream(bytes);
                    request.ContentLength = bytes.Length;
                }
                else
                {
                    // Reset position so downstream readers can consume the original body.
                    request.Body.Seek(0, SeekOrigin.Begin);
                }
                return null;
            }
            else
            {
                var raw = messageElement.GetRawText();
                return JsonSerializer.Deserialize<JsonRpcMessage>(raw, options);
            }
        }
        finally
        {
            if (!unwrapOnly)
            {
                // Reset the request body so it can be read later by other components if necessary.
                request.Body.Seek(0, SeekOrigin.Begin);
            }
        }
    }

    internal static async ValueTask<JsonRpcMessage?> ExtractJsonRpcMessageSseAsync(HttpRequest request, JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        return await ProcessJsonRpcPayloadAsync(request, options, cancellationToken, unwrapOnly: false);
    }

    internal static async ValueTask ExtractJsonRpcMessageHttpStreamableAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        await ProcessJsonRpcPayloadAsync(request, JsonSerializerOptions.Default, cancellationToken, unwrapOnly: true);
    }
}
