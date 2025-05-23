// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using System.Buffers;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class MessageHandler(Stream eventStream, string id) : IMessageHandler, IAsyncDisposable
{
    private readonly Channel<JsonRpcMessage> _incomingChannel = CreateChannel<JsonRpcMessage>();
    private readonly Channel<SseItem<JsonRpcMessage>> _outgoingChannel = CreateChannel<SseItem<JsonRpcMessage>>();
    private Task _handlerTasks = Task.CompletedTask;
    private long _currentRequestId = 0;

    public string Id { get; } = id;

    public Task RunAsync(Func<string, string> endpointWriter, CancellationToken cancellationToken)
    {

        var endpointResponse = new JsonRpcResponse { Id = new RequestId(Id), Result = endpointWriter(Id) };
        _outgoingChannel.Writer.TryWrite(new SseItem<JsonRpcMessage>(endpointResponse, "endpoint"));

        var events = _outgoingChannel.Reader.ReadAllAsync(cancellationToken);
        
        var pingTask = StartPingAsync(cancellationToken);
        var writerTask = SseFormatter.WriteAsync(events, eventStream, BufferWriter, cancellationToken);

        return _handlerTasks = Task.WhenAll(pingTask, writerTask);
    }

    private async Task StartPingAsync(CancellationToken cancellationToken)
    {
        // We're currently pinging to keep the connection alive.
        // Not taking any action if the client fails to respond.
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(25), cancellationToken);

            var requestId = new RequestId($"{Id}-{Interlocked.Increment(ref _currentRequestId)}");
            await SendMessageAsync(new JsonRpcRequest { Id = requestId, Method = "ping" }, cancellationToken);
        }
    }

    private static void BufferWriter(SseItem<JsonRpcMessage> sseItem, IBufferWriter<byte> writer)
    {
        if (string.Equals(sseItem.EventType, "endpoint", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = (((JsonRpcResponse)sseItem.Data).Result?.ToString()) 
                ?? throw new InvalidOperationException("Endpoint is null");

            writer.Write(Encoding.UTF8.GetBytes(endpoint));
            return;
        }

        JsonSerializer.Serialize(new Utf8JsonWriter(writer), sseItem.Data, McpJsonSerializerOptions.DefaultOptions);
    }

    public ChannelReader<JsonRpcMessage> MessageReader => _incomingChannel.Reader;

    public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        => _outgoingChannel.Writer.WriteAsync(new SseItem<JsonRpcMessage>(message), cancellationToken).AsTask();

    public Task ProcessMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        => _incomingChannel.Writer.WriteAsync(message, cancellationToken).AsTask();

    private static Channel<T> CreateChannel<T>()
        => Channel.CreateBounded<T>(new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = false });

    public ValueTask DisposeAsync()
    {
        _incomingChannel.Writer.TryComplete();
        _outgoingChannel.Writer.TryComplete();

        return new ValueTask(_handlerTasks);
    }
}