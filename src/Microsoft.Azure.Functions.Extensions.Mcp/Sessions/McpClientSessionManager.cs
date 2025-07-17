// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;
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

    public IMcpClientSession<TTransport> CreateSession<TTransport>(string clientId, string instanceId, TTransport transport) where TTransport : ITransportWithMessageHandling
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

    private sealed class McpClientSessionImplementation<TTransport>(McpClientSessionManager manager, string clientId, string instanceId, TTransport transport) : IMcpClientSession<TTransport>
        where TTransport : ITransportWithMessageHandling
    {
        private readonly object _pingLock = new();
        private CancellationTokenSource? _pingTokenSource;
        private Task? _pingTask;
        private bool _disposed;

        private async Task StartPingAsync(CancellationToken cancellationToken)
        {
            if (Server is null)
            {
                throw new InvalidOperationException("Server must be set before starting ping.");
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(25), cancellationToken);

                    await Server.SendRequestAsync(new JsonRpcRequest { Method = "ping" }, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation behavior, nothing to do here.
            }
        }

        private Task CancelAndDisposePingAsync()
        {
            if (_pingTokenSource is not null)
            {
                _pingTokenSource.Cancel();
                _pingTokenSource.Dispose();
                _pingTokenSource = null;
            }

            if (_pingTask is not null)
            {
                return Interlocked.Exchange(ref _pingTask, null);
            }

            return Task.CompletedTask;
        }

        public TTransport Transport { get; } = transport;

        public string ClientId { get; } = clientId;

        public string InstanceId { get; } = instanceId;

        public IMcpServer? Server { get; set; }

        public Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
            => Transport.HandleMessageAsync(message, cancellationToken);

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

            await CancelAndDisposePingAsync()
                .ConfigureAwait(false);

            await Transport.DisposeAsync();
        }

        public void StartPing(CancellationToken cancellationToken)
        {
            lock (_pingLock)
            {
                var pingTokenSource = new CancellationTokenSource();
                Interlocked.Exchange(ref _pingTokenSource, pingTokenSource);

                var token = CancellationTokenSource.CreateLinkedTokenSource(_pingTokenSource!.Token, cancellationToken).Token;
                _pingTask = StartPingAsync(token);
            }
        }

        public void StopPing()
        {
            lock (_pingLock)
            {
                _ = CancelAndDisposePingAsync();
            }
        }
    }
}
