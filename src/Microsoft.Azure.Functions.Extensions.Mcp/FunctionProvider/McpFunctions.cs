// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpFunctions
{
    public static Task HandleSseRequest(HttpRequest request)
        => HandleRequest(request.HttpContext);

    public static Task HandleMessageRequest(HttpRequest request)
        => HandleRequest(request.HttpContext);

    private static Task HandleRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestHandler = context.RequestServices.GetService(typeof(ISseRequestHandler)) as ISseRequestHandler
            ?? throw new InvalidOperationException("Request handler not found.");

        return requestHandler.HandleRequest(context);
    }
}
