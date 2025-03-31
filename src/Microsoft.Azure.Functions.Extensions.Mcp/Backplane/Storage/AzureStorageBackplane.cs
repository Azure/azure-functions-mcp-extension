using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;
internal class AzureStorageBackplane : IMcpBackplane
{
    public ChannelReader<IJsonRpcMessage> Messages => throw new NotImplementedException();

    public Task SendMessageAsync(IJsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}