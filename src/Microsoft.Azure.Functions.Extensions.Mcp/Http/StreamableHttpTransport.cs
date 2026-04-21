// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

internal sealed class StreamableHttpTransport : McpExtensionTransport<StreamableHttpServerTransport>
{
    public StreamableHttpTransport(
        string? sessionId = null,
        bool stateless = false,
        bool flowExecutionContext = false,
        Func<InitializeRequestParams, CancellationToken, ValueTask>? onSessionInitialized = null)
        : base(new StreamableHttpServerTransport()
        {
            SessionId = sessionId,
            Stateless = stateless,
            FlowExecutionContextFromRequests = flowExecutionContext,
            OnSessionInitialized = onSessionInitialized,
        })
    {
    }

    public override Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    // SessionId is init-only on StreamableHttpServerTransport (SDK 1.2.0),
    // so we delegate the getter to the SDK transport and reject post-construction mutation.
    public override string? SessionId
    {
        get => Transport.SessionId;
        set => throw new NotSupportedException("SessionId is immutable after transport construction.");
    }

    public Task HandleGetRequestAsync(Stream sseResponseStream, CancellationToken cancellationToken)
        => Transport.HandleGetRequestAsync(sseResponseStream, cancellationToken);

    public Task<bool> HandlePostRequestAsync(JsonRpcMessage message, Stream responseStream, CancellationToken cancellationToken)
        => Transport.HandlePostRequestAsync(message, responseStream, cancellationToken);

    public bool FlowExecutionContextFromRequests => Transport.FlowExecutionContextFromRequests;
}
