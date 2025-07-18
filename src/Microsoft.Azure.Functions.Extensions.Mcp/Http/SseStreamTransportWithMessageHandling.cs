// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;

internal sealed class SseStreamTransportWithMessageHandling(Stream sseStream, string? messageEndpoint = "/message")
           : TransportWithMessageHandling<SseResponseStreamTransport>(new SseResponseStreamTransport(sseStream, messageEndpoint))
{
    public override Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        return Transport.OnMessageReceivedAsync(message, cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken) => Transport.RunAsync(cancellationToken);
}
