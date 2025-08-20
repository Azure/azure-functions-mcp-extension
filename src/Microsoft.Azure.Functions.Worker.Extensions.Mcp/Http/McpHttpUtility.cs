// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Utility class for HTTP operations in MCP extension
/// </summary>
internal sealed class McpHttpUtility
{
    /// <summary>
    /// Tries to get the function key from the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="code">The function key if found</param>
    /// <returns>True if function key was found, false otherwise</returns>
    internal static bool TryGetFunctionKey(HttpContext context, out string? code)
    {
        return TryGetQueryValue(context, FunctionsCodeQuery, out code) ||
               context.Request.Headers.TryGetValue(FunctionsKeyHeader, out StringValues values) &&
               (code = values.FirstOrDefault()) is not null;
    }

    /// <summary>
    /// Tries to get a query parameter value from the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="key">The query parameter key</param>
    /// <param name="value">The query parameter value if found</param>
    /// <returns>True if the query parameter was found, false otherwise</returns>
    internal static bool TryGetQueryValue(HttpContext context, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (context.Request.Query.TryGetValue(key, out var strings))
        {
            value = strings.FirstOrDefault();
        }

        return value is not null;
    }

    /// <summary>
    /// Sets the appropriate headers and features for Server-Sent Events
    /// </summary>
    /// <param name="context">The HTTP context</param>
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
