using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpFunctions(IMcpRequestHandler requestHandler)
{
    public async Task HandleSseRequest(HttpRequest request, ExecutionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestHandler.HandleSseRequest(request.HttpContext);
    }

    public async Task HandleMessageRequest(HttpRequest request, ExecutionContext ctx)
    {
        await requestHandler.HandleMessageRequest(request.HttpContext);
    }
}