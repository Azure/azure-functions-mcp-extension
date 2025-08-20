// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.IO.Pipelines;
using System.Text.Json.Serialization.Metadata;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpConstants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// Handles StreamableHttp requests for MCP in Azure Functions isolated worker process
/// </summary>
internal sealed class StreamableHttpRequestHandler(
    IMcpInstanceIdProvider instanceIdProvider,
    IMcpClientSessionManager clientSessionManager,
    IServiceProvider applicationServices,
    IOptions<McpServerOptions> mcpServerOptions,
    IOptions<McpOptions> mcpOptions) : IStreamableHttpRequestHandler
{
    private static readonly JsonTypeInfo<JsonRpcError> ErrorTypeInfo =
        (JsonTypeInfo<JsonRpcError>) McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonRpcError));

    /// <summary>
    /// Handles an incoming StreamableHttp request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task HandleRequestAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            return HandlePostRequestAsync(context);
        }

        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        return Task.CompletedTask;
    }

    private async Task HandlePostRequestAsync(HttpContext context)
    {
        if (!await PostHasValidMediaTypesAsync(context))
        {
            return;
        }

        var session = await GetOrCreateSessionAsync(context, mcpOptions.Value);

        if (session is null)
        {
            return;
        }

        try
        {
            McpHttpUtility.SetSseContext(context);

            var responseWritten = await session.Transport.HandlePostRequestAsync(new HttpDuplexPipe(context), context.RequestAborted);

            if (!responseWritten)
            {
                context.Response.Headers.ContentType = (string?)null;
                context.Response.StatusCode = StatusCodes.Status202Accepted;
            }
        }
        finally
        {
            if (session.Transport.IsStateless)
            {
                // In stateless mode, we do not store the session in the session manager.
                // The session is recreated for each request based on the MCP-Session-Id.
                // The session ID is written to the response header in the transport.
                await session.DisposeAsync();
            }
        }
    }

    private async Task<bool> PostHasValidMediaTypesAsync(HttpContext context)
    {
        // As per the MCP spec, ensure that, when using streamable HTTP, the client accepts both
        // application/json and text/event-stream media types.
        var typedHeaders = context.Request.GetTypedHeaders();
        if (!typedHeaders.Accept.Any(h => h.MatchesMediaType("application/json"))
            || !typedHeaders.Accept.Any(h => h.MatchesMediaType("text/event-stream")))
        {
            await WriteJsonRpcErrorAsync(context,
                "Not Acceptable: Client must accept both application/json and text/event-stream",
                StatusCodes.Status406NotAcceptable);

            return false;
        }

        return true;
    }

    private async ValueTask<IMcpClientSession<StreamableHttpTransport>?> GetOrCreateSessionAsync(HttpContext context, McpOptions mcpOptionsValue, bool stateless = true)
    {
        ThrowIfNotStatelessSession(stateless);

        var sessionId = context.Request.Headers[McpSessionIdHeaderName].ToString();

        if (string.IsNullOrEmpty(sessionId))
        {
            return await StartNewSessionAsync(context, stateless);
        }

        if (!ClientStateManager.TryParseUriState(sessionId, out string? clientId, out string? instanceId, mcpOptionsValue.EncryptClientState))
        {
            await WriteJsonRpcErrorAsync(context, "Invalid client state.");
            return null;
        }

        return await RebuildStatelessSessionAsync(context);
    }

    private async ValueTask<IMcpClientSession<StreamableHttpTransport>> StartNewSessionAsync(HttpContext context, bool stateless)
    {
        ThrowIfNotStatelessSession(stateless);

        string clientId = Utility.EmptyId;
        StreamableHttpTransport transport = new()
        {
            IsStateless = true,
            SessionContext = new(clientId, instanceIdProvider.InstanceId),
            OnInitRequestReceived = (t, p) =>
                {
                    var sessionId = Utility.CreateId();
                    var clientState = ClientStateManager.FormatUriState(sessionId, instanceIdProvider.InstanceId, mcpOptions.Value.EncryptClientState);

                    // Persist the session ID in the response header after receiving the initialize request.
                    t.SessionId = sessionId;
                    context.Response.Headers[McpSessionIdHeaderName] = clientState;

                    return ValueTask.CompletedTask;
                }
        };

        // Create a new session with the transport.
        return await CreateSessionAsync(context, transport, clientId);
    }

    private async ValueTask<IMcpClientSession<StreamableHttpTransport>?> RebuildStatelessSessionAsync(HttpContext context)
    {
        var sessionId = context.Request.Headers[McpSessionIdHeaderName].ToString();

        if (!ClientStateManager.TryParseUriState(sessionId, out var clientId, out var instanceId, mcpOptions.Value.EncryptClientState))
        {
            await WriteJsonRpcErrorAsync(context, "Invalid client state.", StatusCodes.Status400BadRequest);
            return null;
        }

        var transport = new StreamableHttpTransport
        {
            IsStateless = true,
            SessionId = clientId,
            SessionContext = new(clientId, instanceIdProvider.InstanceId)
        };

        return await CreateSessionAsync(context, transport, clientId);
    }

    private async ValueTask<IMcpClientSession<StreamableHttpTransport>> CreateSessionAsync(HttpContext context, StreamableHttpTransport transport, string sessionId)
    {
        var sessionServices = applicationServices;
        var serverOptions = mcpServerOptions.Value;

        if (transport.IsStateless)
        {
            sessionServices = context.RequestServices;
        }

        // For now, we'll create a simplified session without the full MCP server factory
        // This is a simplified implementation for the isolated worker context
        var session = await clientSessionManager.CreateSessionAsync(sessionId, instanceIdProvider.InstanceId, transport);
        
        // Note: In a full implementation, you would need to create the MCP server instance here
        // For now, we'll set it to null and handle the MCP protocol more directly
        session.Server = null;

        return session;
    }

    private static Task WriteJsonRpcErrorAsync(HttpContext context, string errorMessage,
        int statusCode = StatusCodes.Status400BadRequest, int errorCode = -32000)
    {
        var jsonRpcError = new JsonRpcError
        {
            Error = new()
            {
                Code = errorCode,
                Message = errorMessage,
            },
        };

        return Results.Json(jsonRpcError, ErrorTypeInfo, statusCode: statusCode).ExecuteAsync(context);
    }

    private static void ThrowIfNotStatelessSession(bool stateless)
    {
        if (!stateless)
        {
            throw new NotSupportedException("Stateful sessions are not supported in Azure Functions isolated worker.");
        }
    }

    private sealed class HttpDuplexPipe(HttpContext context) : IDuplexPipe
    {
        public PipeReader Input => context.Request.BodyReader;
        public PipeWriter Output => context.Response.BodyWriter;
    }
}
