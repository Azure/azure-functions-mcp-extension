using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.Azure.Functions.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultRequestHandler(IMessageHandlerManager messageHandlerManager, IMcpInstanceIdProvider instanceIdProvider) : IRequestHandler
{
    private readonly IMessageHandlerManager _messageHandlerManager = messageHandlerManager;

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
        string clientState = ClientStateManager.FormatUriState(clientId, instanceIdProvider.InstanceId);
        string result = $"message?{AzmcpStateQuery}={clientState}";

        if (TryGetFunctionKey(context, out string? code))
        {
            result += $"&{FunctionsCodeQuery}={code}";
        }

        return result;
    }

    public async Task HandleMessageRequest(HttpContext context)
    {
        static Task WriteInvalidSessionResponse(string message, HttpContext httpContext)
        {
            return Results.BadRequest($"{message} Please connect to the /sse endpoint to initiate your session.").ExecuteAsync(httpContext);
        }

        if (!TryGetQueryValue(context, AzmcpStateQuery, out string? clientState))
        {
            await WriteInvalidSessionResponse("Missing service context.", context);
            return;
        }

        if (!ClientStateManager.TryParseUriState(clientState, out string? clientId, out string? instanceId))
        {
            await WriteInvalidSessionResponse("Invalid client state.", context);
            return;
        }

        var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonSerializerOptions.DefaultOptions, context.RequestAborted);

        if (message is null)
        {
            await Results.BadRequest("No message in request body.").ExecuteAsync(context);
            return;
        }

        await _messageHandlerManager.HandleMessageAsync(message, instanceId, clientId, context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");
    }

    private bool TryGetFunctionKey(HttpContext context, out string? code)
    {
        return TryGetQueryValue(context, FunctionsCodeQuery, out code) ||
               context.Request.Headers.TryGetValue(FunctionsKeyHeader, out StringValues values) &&
               (code = values.FirstOrDefault()) is not null;
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
