// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
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
}
