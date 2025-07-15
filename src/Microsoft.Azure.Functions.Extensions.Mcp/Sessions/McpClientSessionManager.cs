// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpClientSessionManager : IMcpClientSessionManager
{
    private readonly ConcurrentDictionary<string, IMcpClientSession> _sessions = new();

    private void RemoveSession(string clientId)
    {
        if (!_sessions.TryRemove(clientId, out var _))
        {
            // This condition should never happen
            throw new InvalidOperationException($"A session for client '{clientId}' does not exist.");
        }
    }

    public IMcpClientSession<TTransport> CreateSession<TTransport>(string clientId, string instanceId, TTransport transport) where TTransport : ITransport
    {
        var clientSession = new McpClientSessionImplementation<TTransport>(this, clientId, instanceId, transport);

        if (!_sessions.TryAdd(clientId, clientSession))
        {
            // This condition should never happen
            throw new InvalidOperationException($"A session for client '{clientId}' already exists.");
        }

        return clientSession;
    }

    public bool TryGetSession(string clientId, [NotNullWhen(true)] out IMcpClientSession? clientSession)
    {
        return _sessions.TryGetValue(clientId, out clientSession);
    }

    public bool TryGetSession<TTransport>(string clientId, [NotNullWhen(true)] out IMcpClientSession<TTransport>? clientSession) where TTransport : ITransport
    {
        var result = _sessions.TryGetValue(clientId, out var session);
        clientSession = session as IMcpClientSession<TTransport>;
        
        return result;
    }

    private sealed class McpClientSessionImplementation<TTransport>(McpClientSessionManager manager, string clientId, string instanceId, TTransport transport) : IMcpClientSession<TTransport>
        where TTransport : ITransport
    {
        private bool _disposed;

        public TTransport Transport { get; } = transport;

        public string ClientId { get; } = clientId;

        public string InstanceId { get; } = instanceId;

        public IMcpServer? Server { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            manager.RemoveSession(ClientId);

            if (Server is not null)
            {
                await Server.DisposeAsync()
                    .ConfigureAwait(false);

                Server = null;
            }

            await Transport.DisposeAsync();
        }
    }
}
