// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

internal sealed class SseStreamTransportWithMessageHandling(Stream sseStream, string? messageEndpoint = "/message")
           : TransportWithMessageHandling<SseResponseStreamTransport>(new SseResponseStreamTransport(sseStream, messageEndpoint))
{
    public override Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        return Transport.OnMessageReceivedAsync(message, cancellationToken);
    }

    public override string? SessionId
    {
        get => Transport.SessionId;
        set => throw new NotSupportedException("SessionId cannot be set on SseStreamTransportWithMessageHandling.");
    }

    public Task RunAsync(CancellationToken cancellationToken) => Transport.RunAsync(cancellationToken);
}
