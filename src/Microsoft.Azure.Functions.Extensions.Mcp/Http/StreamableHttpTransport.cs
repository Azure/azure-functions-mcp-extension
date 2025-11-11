// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO.Pipelines;
using Microsoft.Azure.Functions.Extensions.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

internal sealed class StreamableHttpTransport(bool flowExecutionContext = false)
    : McpExtensionTransport<StreamableHttpServerTransport>(new StreamableHttpServerTransport() { FlowExecutionContextFromRequests = flowExecutionContext })
{
    private Func<StreamableHttpTransport, InitializeRequestParams?, ValueTask>? _onInitRequestReceived;

    public override Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override string? SessionId
    {
        get => Transport.SessionId;
        set => Transport.SessionId = value;
    }

    public Task HandleGetRequestAsync(Stream sseResponseStream, CancellationToken cancellationToken)
        => Transport.HandleGetRequestAsync(sseResponseStream, cancellationToken);

    public Task<bool> HandlePostRequestAsync(JsonRpcMessage message, Stream responseStream, CancellationToken cancellationToken)
        => Transport.HandlePostRequestAsync(message, responseStream, cancellationToken);

    public bool FlowExecutionContextFromRequests => Transport.FlowExecutionContextFromRequests;

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
