// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpClientSessionManager(ILogger<McpClientSessionManager> logger) : IMcpClientSessionManager
{
    private readonly ConcurrentDictionary<string, IMcpClientSession> _sessions = new();

    private void RemoveSession(string clientId)
    {
        if (!_sessions.TryRemove(clientId, out var _))
        {
            // This condition should never happen
            throw new InvalidOperationException($"A session for client '{clientId}' does not exist.");
        }

        logger.LogInformation("Removed session for client '{ClientId}'.", clientId);
    }

    public IMcpClientSession<TTransport> CreateSession<TTransport>(string clientId, string instanceId, TTransport transport) where TTransport : ITransportWithMessageHandling
    {
        var clientSession = new McpClientSessionImplementation<TTransport>(this, clientId, instanceId, transport);

        if (!_sessions.TryAdd(clientId, clientSession))
        {
            // This condition should never happen
            throw new InvalidOperationException($"A session for client '{clientId}' already exists.");
        }

        logger.LogInformation("Created session for client '{ClientId}' with instance '{InstanceId}'.", clientId, instanceId);

        return clientSession;
    }

    public bool TryGetSession(string clientId, [NotNullWhen(true)] out IMcpClientSession? clientSession)
    {
        return _sessions.TryGetValue(clientId, out clientSession);
    }

    private sealed class McpClientSessionImplementation<TTransport>(McpClientSessionManager manager, string clientId, string instanceId, TTransport transport) : IMcpClientSession<TTransport>
        where TTransport : ITransportWithMessageHandling
    {
        private readonly SemaphoreSlim _pingSemaphore = new(1, 1);
        private CancellationTokenSource? _pingTokenSource;
        private Task? _pingTask;
        private bool _disposed;

        public TTransport Transport { get; } = transport;

        public string ClientId { get; } = clientId;

        public string InstanceId { get; } = instanceId;

        public IMcpServer? Server { get; set; }

        public Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
            => Transport.HandleMessageAsync(message, cancellationToken);

        private async Task StartPingCoreAsync(CancellationToken cancellationToken)
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

        private async Task CancelAndDisposePingAsync()
        {
            using var _ = await _pingSemaphore.LockAsync().ConfigureAwait(false);

            if (_pingTokenSource is not null)
            {
                _pingTokenSource.Cancel();
                _pingTokenSource.Dispose();
                _pingTokenSource = null;
            }

            var pingTask = Interlocked.Exchange(ref _pingTask, null);

            if (pingTask is not null)
            {
                await pingTask.ConfigureAwait(false);
            }
        }

        public async Task StartPingAsync(CancellationToken cancellationToken)
        {
            using var _ = await _pingSemaphore.LockAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_pingTask is not null && !_pingTask.IsCompleted)
            {
                // Ping is already running, no need to start it again.
                return;
            }

            _pingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pingTask = StartPingCoreAsync(_pingTokenSource.Token);
        }

        public async Task StopPingAsync()
        {
            await CancelAndDisposePingAsync();
        }

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

            await Transport.DisposeAsync()
                .ConfigureAwait(false);
        }
    }
}
