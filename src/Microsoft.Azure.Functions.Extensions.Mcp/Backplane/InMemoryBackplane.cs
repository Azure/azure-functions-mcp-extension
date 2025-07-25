// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane;

internal class InMemoryBackplane : IMcpBackplane, IAsyncDisposable
{
    private readonly Channel<McpBackplaneMessage> _mesageChannel = Channel.CreateUnbounded<McpBackplaneMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<McpBackplaneMessage> Messages => _mesageChannel.Reader;

    public Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        _mesageChannel.Writer.WriteAsync(new McpBackplaneMessage { ClientId = clientId, Message = message });

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _mesageChannel.Writer.TryComplete();

        return ValueTask.CompletedTask;
    }
}
