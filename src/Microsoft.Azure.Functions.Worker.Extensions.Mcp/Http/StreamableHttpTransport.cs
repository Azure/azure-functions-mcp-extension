// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO.Pipelines;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;

/// <summary>
/// StreamableHttp transport implementation for Azure Functions isolated worker process
/// </summary>
internal sealed class StreamableHttpTransport(bool flowExecutionContext = false)
    : McpExtensionTransport<StreamableHttpServerTransport>(new StreamableHttpServerTransport() { FlowExecutionContextFromRequests = flowExecutionContext })
{
    private Func<StreamableHttpTransport, InitializeRequestParams?, ValueTask>? _onInitRequestReceived;

    /// <summary>
    /// Handles a message asynchronously - not supported in StreamableHttp mode
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="NotSupportedException">StreamableHttp doesn't support direct message handling</exception>
    public override Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("StreamableHttp transport doesn't support direct message handling.");
    }

    /// <summary>
    /// Gets or sets the session ID
    /// </summary>
    public override string? SessionId
    {
        get => Transport.SessionId;
        set => Transport.SessionId = value;
    }

    /// <summary>
    /// Handles a GET request asynchronously (for SSE fallback)
    /// </summary>
    /// <param name="sseResponseStream">The SSE response stream</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task HandleGetRequestAsync(Stream sseResponseStream, CancellationToken cancellationToken)
        => Transport.HandleGetRequest(sseResponseStream, cancellationToken);

    /// <summary>
    /// Handles a POST request asynchronously
    /// </summary>
    /// <param name="httpBodies">The HTTP bodies duplex pipe</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing whether a response was written</returns>
    public Task<bool> HandlePostRequestAsync(IDuplexPipe httpBodies, CancellationToken cancellationToken)
        => Transport.HandlePostRequest(httpBodies, cancellationToken);

    /// <summary>
    /// Gets whether to flow execution context from requests
    /// </summary>
    public bool FlowExecutionContextFromRequests => Transport.FlowExecutionContextFromRequests;

    /// <summary>
    /// Gets or sets the callback for when an initialization request is received
    /// </summary>
    public Func<StreamableHttpTransport, InitializeRequestParams?, ValueTask>? OnInitRequestReceived
    {
        get => _onInitRequestReceived;
        set
        {
            _onInitRequestReceived = value;
            Transport.OnInitRequestReceived = requestParams
                => _onInitRequestReceived?.Invoke(this, requestParams) ?? ValueTask.CompletedTask;
        }
    }
}
