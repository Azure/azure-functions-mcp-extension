using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpBackplane
{
    ChannelReader<McpBackplaneMessage> Messages { get; }

    Task SendMessageAsync(IJsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken);
}