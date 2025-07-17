// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Server;
using static Microsoft.Azure.Functions.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class SseRequestHandler(
    IMcpInstanceIdProvider instanceIdProvider,
    IMcpClientSessionManager clientSessionManager,
    IMcpBackplaneService backplaneService,
    IOptions<McpOptions> mcpOptions,
    IOptions<McpServerOptions> mcpServerOptions,
    ILoggerFactory loggerFactory) : ISseRequestHandler
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Logs.SseRequestHandler>();

    public bool IsSseRequest(HttpContext context)
    {
        var pathSpan = context.Request.Path.Value.AsSpan().TrimEnd('/');

        return pathSpan.EndsWith(SseEndpoint, StringComparison.OrdinalIgnoreCase)
               || pathSpan.EndsWith(MessageEndpoint, StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleRequest(HttpContext context)
    {
        if (context.Request.Path.Value.AsSpan().TrimEnd('/').EndsWith(SseEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            await HandleSseRequest(context);
        }
        else
        {
            await HandleMessageRequest(context, mcpOptions.Value);
        }
    }

    public async Task HandleSseRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        McpHttpUtility.SetSseContext(context);

        var clientId = Utility.CreateId();
        var messageEndpoint = GetMessageEndpoint(clientId, context);
        var transport = new SseStreamTransportWithMessageHandling(context.Response.Body, messageEndpoint);

        await using var clientSession = clientSessionManager.CreateSession(clientId, instanceIdProvider.InstanceId, transport);

        try
        {
            _logger.LogInformation("Handling SSE request for client '{clientId}'.", clientId);

            var transportTask = transport.RunAsync(context.RequestAborted);

            try
            {
                await using var mcpServer = McpServerFactory.Create(transport, mcpServerOptions.Value, loggerFactory, context.RequestServices);
                clientSession.Server = mcpServer;
                context.Features.Set(mcpServer);

                clientSession.StartPing(context.RequestAborted);
                
                await mcpServer.RunAsync(context.RequestAborted);
            }
            finally
            {
                await transport.DisposeAsync();
                await transportTask;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Nothing to do. Normal client disconnect behavior...
        }
    }

    public async Task HandleMessageRequest(HttpContext context, McpOptions mcpOptions)
    {
        if (!McpHttpUtility.TryGetQueryValue(context, AzmcpStateQuery, out string? clientState))
        {
            await WriteInvalidSessionResponse("Missing service context.", context);
            return;
        }

        if (!ClientStateManager.TryParseUriState(clientState, out string? clientId, out string? instanceId, mcpOptions.EncryptClientState))
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

        if (clientSessionManager.TryGetSession(clientId, out var clientSession))
        {
            await clientSession.HandleMessageAsync(message, context.RequestAborted);
        }
        else
        {
            // If we're unable to find the session, but the client state indicates it was bound to this instance, we have an error condition
            // as that client ID is not valid.
            if (string.Equals(instanceId, instanceIdProvider.InstanceId, StringComparison.OrdinalIgnoreCase))
            {
                await WriteInvalidSessionResponse($"No active session for client '{clientId}' and instance '{instanceId}'.", context);
                return;
            }

            await backplaneService.SendMessageAsync(message, instanceId, clientId, context.RequestAborted);
        }

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        await context.Response.WriteAsync("Accepted");

        static Task WriteInvalidSessionResponse(string message, HttpContext httpContext)
        {
            return Results.BadRequest($"{message} Please connect to the /sse endpoint to initiate your session.").ExecuteAsync(httpContext);
        }
    }

    internal string GetMessageEndpoint(string clientId, HttpContext context)
    {
        var clientState = ClientStateManager.FormatUriState(clientId, instanceIdProvider.InstanceId, mcpOptions.Value.EncryptClientState);
        var result = $"{MessageEndpoint}?{AzmcpStateQuery}={clientState}";

        if (McpHttpUtility.TryGetFunctionKey(context, out string? code))
        {
            result += $"&{FunctionsCodeQuery}={code}";
        }

        if (mcpOptions.Value.MessageOptions.UseAbsoluteUriForEndpoint)
        {
            var request = context.Request;

            var fullPath = $"{request.PathBase}{request.Path}".TrimEnd('/');

            var lastSegmentDelimiter = fullPath.LastIndexOf('/');
            var trimmedPath = lastSegmentDelimiter > 0 ? fullPath[..lastSegmentDelimiter] : string.Empty;

            result = $"{request.Scheme}://{request.Host}{trimmedPath}/{result}";
        }

        return result;
    }
}
