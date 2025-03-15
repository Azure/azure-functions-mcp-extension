using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpRequestHandler
{
    Task HandleSseRequest(HttpContext context);

    Task HandleMessageRequest(HttpContext context);
}