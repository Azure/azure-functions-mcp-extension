using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IMessageHandler
{
    string Id { get; }

    ChannelReader<IJsonRpcMessage> MessageReader { get; }

    Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken);

    Task ProcessMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);
}