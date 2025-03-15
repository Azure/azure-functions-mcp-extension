using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public sealed class DefaultMcpRequestHandler : IMcpRequestHandler
{
    private readonly Dictionary<string, MessageHandler> _handlers = new Dictionary<string, MessageHandler>();
    
    public async Task HandleSseRequest(HttpContext context)
    {
        // Set the appropriate headers for SSE.
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Connection", "keep-alive");

        // Keep sending data as long as the client is connected.
        var counter = 0;

        while (!context.RequestAborted.IsCancellationRequested)
        {
            counter++;
            // Format the SSE data. Each event is separated by two newlines.
            await context.Response.WriteAsync($"data: Server message {counter}\n\n", cancellationToken: context.RequestAborted);

            // Flush the data to ensure it gets sent to the client immediately.
            await context.Response.Body.FlushAsync(context.RequestAborted);

            // Wait for 1 second before sending the next event.
            await Task.Delay(1000, context.RequestAborted);
        }
    }

    public Task HandleMessageRequest(HttpContext context)
    {
        throw new NotImplementedException();
    }
}