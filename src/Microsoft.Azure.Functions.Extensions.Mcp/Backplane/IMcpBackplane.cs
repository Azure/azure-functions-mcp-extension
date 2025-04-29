using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using System.Threading.Channels;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpBackplane
{
    ChannelReader<McpBackplaneMessage> Messages { get; }

    Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken);
}