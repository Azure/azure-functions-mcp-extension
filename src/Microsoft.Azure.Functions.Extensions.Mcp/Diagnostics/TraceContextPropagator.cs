// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Extracts W3C Trace Context from MCP request params._meta property.
/// </summary>
internal static class TraceContextPropagator
{
    private const string TraceparentKey = "traceparent";
    private const string TracestateKey = "tracestate";

    /// <summary>
    /// Extracts W3C trace context from the request params._meta property.
    /// </summary>
    public static ActivityContext? Extract(RequestParams? requestParams)
    {
        var meta = requestParams?.Meta;
        if (meta is null)
        {
            return null;
        }

        if (!TryGetString(meta, TraceparentKey, out var traceparent))
        {
            return null;
        }

        TryGetString(meta, TracestateKey, out var tracestate);

        if (ActivityContext.TryParse(traceparent, tracestate, out var context))
        {
            return context;
        }

        return null;
    }

    private static bool TryGetString(JsonObject meta, string key, out string? value)
    {
        value = null;

        if (meta.TryGetPropertyValue(key, out var node) && node is JsonValue jsonValue)
        {
            return jsonValue.TryGetValue(out value);
        }

        return false;
    }
}
