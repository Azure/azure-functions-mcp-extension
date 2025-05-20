using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using ModelContextProtocol.Protocol.Messages;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.Azure.Functions.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultRequestHandler(IMessageHandlerManager messageHandlerManager, IMcpInstanceIdProvider instanceIdProvider, IOptions<McpOptions> mcpOptions, ILogger<Logs.DefaultRequestHandler> logger) : IRequestHandler
{
    public async Task HandleSseRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        // Set the appropriate headers for SSE.
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache,no-store");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.ContentEncoding = "identity";
        context.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();

        IMessageHandler handler = messageHandlerManager.CreateHandler(context.Response.Body, context.RequestAborted);

        try
        {
            logger.LogInformation("Handling SSE request for client '{clientId}'.", handler.Id);

            await handler.RunAsync(clientId => WriteEndpoint(clientId, context), context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Nothing to do. Normal client disconnect behavior...
        }
        finally
        {
            try
            {
                await messageHandlerManager.CloseHandlerAsync(handler);
            }
            catch (Exception)
            {
                // Ignore exceptions during handler closure.
                // Do we want to log this cleanup?
            }
        }
    }

    internal string WriteEndpoint(string clientId, HttpContext context)
    {
        var mcpOptionsValue = mcpOptions.Value;
        var clientState = ClientStateManager.FormatUriState(clientId, instanceIdProvider.InstanceId, mcpOptionsValue.EncryptClientState);
        var result = $"message?{AzmcpStateQuery}={clientState}";

        if (TryGetFunctionKey(context, out string? code))
        {
            result += $"&{FunctionsCodeQuery}={code}";
        }

        if (mcpOptionsValue.MessageOptions.UseAbsoluteUriForEndpoint)
        {
            var request = context.Request;

            var fullPath = $"{request.PathBase}{request.Path}".TrimEnd('/');

            var lastSegmentDelimiter = fullPath.LastIndexOf('/');
            var trimmedPath = lastSegmentDelimiter > 0 ? fullPath[..lastSegmentDelimiter] : string.Empty;

            result = $"{request.Scheme}://{request.Host}{trimmedPath}/{result}";
        }

        return result;
    }

    public async Task HandleMessageRequest(HttpContext context)
    {
        if (!TryGetQueryValue(context, AzmcpStateQuery, out string? clientState))
        {
            await WriteInvalidSessionResponse("Missing service context.", context);
            return;
        }

        if (!ClientStateManager.TryParseUriState(clientState, out string? clientId, out string? instanceId, mcpOptions.Value.EncryptClientState))
        {
            await WriteInvalidSessionResponse("Invalid client state.", context);
            return;
        }

        var message = await context.Request.ReadFromJsonAsync<JsonRpcMessage>(McpJsonSerializerOptions.DefaultOptions, context.RequestAborted);

        if (message is null)
        {
            await Results.BadRequest("No message in request body.").ExecuteAsync(context);
            return;
        }

        await messageHandlerManager.HandleMessageAsync(message, instanceId, clientId, context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");
        return;

        static Task WriteInvalidSessionResponse(string message, HttpContext httpContext)
        {
            return Results.BadRequest($"{message} Please connect to the /sse endpoint to initiate your session.").ExecuteAsync(httpContext);
        }
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
