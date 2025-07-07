// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class McpClientSession<TTransport>(string clientId, string instanceId, TTransport transport) : IAsyncDisposable
    where TTransport : ITransport
{
    public string ClientId { get; } = clientId;

    public string InstanceId { get; } = instanceId;

    public TTransport Transport { get; } = transport;

    public IMcpServer? Server { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (Server is not null)
        {
            await Server.DisposeAsync();
        }

        await Transport.DisposeAsync();
    }
}
