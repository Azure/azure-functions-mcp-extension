using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultRequestHandler(IMessageHandlerManager messageHandlerManager) : IRequestHandler
{
    public async Task HandleSseRequest(HttpContext context)
    {
        // Set the appropriate headers for SSE.
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        IMessageHandler handler = messageHandlerManager.CreateHandler(context.Response.Body, context.RequestAborted);

        try
        {
            await handler.StartAsync(context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Nothing to do. Normal client disconnect behavior...
        }
        finally
        {
            await messageHandlerManager.CloseHandlerAsync(handler);
        }
    }

    public async Task HandleMessageRequest(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue("mcpcid", out StringValues mcpClientId)
            || !messageHandlerManager.TryGetHandler(mcpClientId!, out IMessageHandler? handler))
        {
            await Results.BadRequest("Missing client context. Please connect to the /sse endpoint to initiate your session.").ExecuteAsync(context);
            return;
        }

        var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonSerializerOptions.DefaultOptions, context.RequestAborted);

        if (message is null)
        {
            await Results.BadRequest("No message in request body.").ExecuteAsync(context);
            return;
        }

        await handler.ProcessMessageAsync(message, context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");
    }
}