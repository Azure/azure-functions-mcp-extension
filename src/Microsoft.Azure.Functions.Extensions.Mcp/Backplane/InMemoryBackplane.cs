using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane;

internal class InMemoryBackplane : IMcpBackplane
{
    private readonly Channel<McpBackplaneMessage> _mesageChannel = Channel.CreateUnbounded<McpBackplaneMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<McpBackplaneMessage> Messages => _mesageChannel.Reader;

    public Task SendMessageAsync(IJsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        _mesageChannel.Writer.TryWrite(new McpBackplaneMessage { ClientId = clientId, Message = message});

        return Task.CompletedTask;
    }
}