// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    internal abstract class TransportWithMessageHandling<TTransport> : ITransportWithMessageHandling
        where TTransport : class, ITransport
    {
        private readonly TTransport _transport;

        public TransportWithMessageHandling(TTransport transport)
        {
            ArgumentNullException.ThrowIfNull(transport);
            _transport = transport;
        }

        public abstract string? SessionId { get; set; }

        public abstract Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);

        public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
            => _transport.SendMessageAsync(message, cancellationToken);

        public ChannelReader<JsonRpcMessage> MessageReader => _transport.MessageReader;

        protected TTransport Transport => _transport;

        public ValueTask DisposeAsync() => _transport.DisposeAsync();
    }
}
