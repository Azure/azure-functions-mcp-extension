// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed partial class DefaultRequestHandler(IStreamableHttpRequestHandler streamableHttpRequestHandler, ISseRequestHandler sseRequestHandler) : IMcpRequestHandler
{
    public async Task HandleRequest(HttpContext context)
    {
        if (sseRequestHandler.IsLegacySseRequest(context))
        {
            await sseRequestHandler.HandleRequest(context);
        }
        else
        {
            await streamableHttpRequestHandler.HandleRequestAsync(context);
        }
    }
}
