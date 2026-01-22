// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class McpTriggerTransportHelper
{
    public static Transport GetTransportInformation(IServiceProvider? services)
    {
        if (services?.GetService(typeof(IHttpContextAccessor)) is IHttpContextAccessor contextAccessor
            && contextAccessor.HttpContext is not null)
        {
            var name = contextAccessor.HttpContext.Items[McpConstants.McpTransportName] as string ?? "http";

            var transport = new Transport
            {
                Name = name
            };

            var headers = contextAccessor.HttpContext.Request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value!), StringComparer.OrdinalIgnoreCase);
            transport.Properties.Add("headers", headers);

            if (headers.TryGetValue(McpConstants.McpSessionIdHeaderName, out var sessionId))
            {
                transport.SessionId = sessionId;
            }

            return transport;
        }

        return new Transport
        {
            Name = "unknown"
        };
    }
}
