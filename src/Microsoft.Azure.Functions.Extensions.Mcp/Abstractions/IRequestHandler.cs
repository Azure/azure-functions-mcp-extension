using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IRequestHandler
{
    Task HandleSseRequest(HttpContext context);

    Task HandleMessageRequest(HttpContext context);
}