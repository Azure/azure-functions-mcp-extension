using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultRequestHandler : IRequestHandler
{
    private readonly string InstanceId = Guid.NewGuid().ToString();
    private readonly IMessageHandlerManager _messageHandlerManager;
    private readonly IMcpBackplane _backplane;

    public DefaultRequestHandler(IMessageHandlerManager messageHandlerManager, IMcpBackplane backplane)
    {
        _messageHandlerManager = messageHandlerManager;
        _backplane = backplane;
    }

    public async Task HandleSseRequest(HttpContext context)
    {
        // Set the appropriate headers for SSE.
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        IMessageHandler handler = _messageHandlerManager.CreateHandler(context.Response.Body, context.RequestAborted);

        try
        {
            await handler.StartAsync(context.RequestAborted, (clientId) => WriteEndpoint(clientId, context));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Nothing to do. Normal client disconnect behavior...
        }
        finally
        {
            try
            {
                await _messageHandlerManager.CloseHandlerAsync(handler);
            }
            catch (Exception)
            {
                // Ignore exceptions during handler closure.
                // Do we want to log this cleanup?
            }
        }
    }

    private string WriteEndpoint(string clientId, HttpContext context)
    {
        if (context.Request.Query.TryGetValue("code", out StringValues code))
        {
            return $"message?azmcpcid={clientId}&azmcpiid={InstanceId}&code={code}";
        }

        return $"message?azmcpcid={clientId}&azmcpiid={InstanceId}";
    }

    public async Task HandleMessageRequest(HttpContext context)
    {
        static Task WriteInvalidSessionResponse(string message, HttpContext httpContext)
        {
            return Results.BadRequest($"{message} Please connect to the /sse endpoint to initiate your session.").ExecuteAsync(httpContext);
        }

        if (!TryGetQueryValue(context, "azmciid", out string? instanceId))
        {
            await WriteInvalidSessionResponse("Missing service context.", context);
            return;
        }

        if (!TryGetQueryValue(context, "azmcpcid", out string? mcpClientId))
        {
            await WriteInvalidSessionResponse("Missing client context.", context);
            return;
        }

        var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonSerializerOptions.DefaultOptions, context.RequestAborted);

        if (message is null)
        {
            await Results.BadRequest("No message in request body.").ExecuteAsync(context);
            return;
        }

        if (string.Equals(instanceId, InstanceId, StringComparison.OrdinalIgnoreCase))
        {
            if (!_messageHandlerManager.TryGetHandler(mcpClientId!, out IMessageHandler? handler))
            {
                await WriteInvalidSessionResponse("Invalid client context.", context);
                return;
            }

            await handler.ProcessMessageAsync(message, context.RequestAborted);
        }
        else
        {
            // Route this request to the appropriate instance
            await _backplane.SendMessageAsync(message, instanceId, mcpClientId, context.RequestAborted);
        }

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");
    }

    private static bool TryGetQueryValue(HttpContext context, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (context.Request.Query.TryGetValue(key, out var strings))
        {
            value = strings.FirstOrDefault();
        }

        return value is not null;
    }
}