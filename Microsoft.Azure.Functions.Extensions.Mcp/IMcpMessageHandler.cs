using System.Threading.Channels;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpMessageHandler
{
    string Id { get; }

    ChannelReader<IJsonRpcMessage> MessageReader { get; }

    Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken);

    Task ProcessMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);
}