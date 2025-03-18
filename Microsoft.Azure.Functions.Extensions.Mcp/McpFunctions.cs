using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpFunctions(IMcpRequestHandler requestHandler)
{
    public static async Task HandleSseRequest([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "sse")] HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestHandler = request.HttpContext.RequestServices.GetService(typeof(IMcpRequestHandler)) as IMcpRequestHandler;

        if (requestHandler == null)
        {
            throw new InvalidOperationException("Request handler not found.");
        }

        await requestHandler.HandleSseRequest(request.HttpContext);
    }

    public async Task HandleMessageRequest(HttpRequest request)
    {
        await requestHandler.HandleMessageRequest(request.HttpContext);
    }
}