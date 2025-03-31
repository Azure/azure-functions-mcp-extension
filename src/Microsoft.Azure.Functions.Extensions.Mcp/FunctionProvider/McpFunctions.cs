using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpFunctions
{
    public static async Task HandleSseRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestHandler = request.HttpContext.RequestServices.GetService(typeof(IRequestHandler)) as IRequestHandler
            ?? throw new InvalidOperationException("Request handler not found.");

        await requestHandler.HandleSseRequest(request.HttpContext);
    }

    public static async Task HandleMessageRequest(HttpRequest request)
    {
        var requestHandler = request.HttpContext.RequestServices.GetService(typeof(IRequestHandler)) as IRequestHandler
            ?? throw new InvalidOperationException("Request handler not found.");

        await requestHandler.HandleMessageRequest(request.HttpContext);
    }
}