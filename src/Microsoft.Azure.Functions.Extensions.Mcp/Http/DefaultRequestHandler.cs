// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed partial class DefaultRequestHandler(IStreamableHttpRequestHandler streamableHttpRequestHandler, ISseRequestHandler sseRequestHandler, ILoggerFactory loggerFactory) : IMcpRequestHandler
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Logs.DefaultRequestHandler>();

    public async Task HandleRequest(HttpContext context)
    {
        if (sseRequestHandler.IsSseRequest(context))
        {
            logger.LogInformation("Handling legacy SSE request.");
            await sseRequestHandler.HandleRequest(context);
        }
        else
        {
            await streamableHttpRequestHandler.HandleRequest(context);
        }
    }
}
