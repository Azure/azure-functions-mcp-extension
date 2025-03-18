using System.Threading.Channels;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpMessageHandler
{
    string Id { get; }

    ChannelReader<string> MessageReader { get; }

    Task SendMessageAsync(string message, CancellationToken cancellationToken);

    Task ProcessMessageAsync(string message, CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);
}