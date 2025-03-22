using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using System.Buffers;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class MessageHandler(Stream eventStream) : IMessageHandler, IAsyncDisposable
{
    private readonly Channel<IJsonRpcMessage> _incomingChannel = CreateChannel<IJsonRpcMessage>();
    private readonly Channel<SseItem<IJsonRpcMessage>> _outgoingChannel = CreateChannel<SseItem<IJsonRpcMessage>>();
    private Task _writeTask = Task.CompletedTask;

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _outgoingChannel.Writer.TryWrite(new SseItem<IJsonRpcMessage>(null!, "endpoint") { EventId = Id });

        var events = _outgoingChannel.Reader.ReadAllAsync(cancellationToken);

        return _writeTask = SseFormatter.WriteAsync(events, eventStream, BufferWriter, cancellationToken);
    }

    private static void BufferWriter<T>(SseItem<T> sseItem, IBufferWriter<byte> writer)
    {
        if (string.Equals(sseItem.EventType, "endpoint", StringComparison.OrdinalIgnoreCase))
        {
            writer.Write(Encoding.UTF8.GetBytes($"message?mcpcid={sseItem.EventId}"));
            return;
        }

        JsonSerializer.Serialize(new Utf8JsonWriter(writer), sseItem.Data, McpJsonSerializerOptions.DefaultOptions);
    }

    public ChannelReader<IJsonRpcMessage> MessageReader => _incomingChannel.Reader;

    public Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
        => _outgoingChannel.Writer.WriteAsync(new SseItem<IJsonRpcMessage>(message), cancellationToken).AsTask();

    public Task ProcessMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
        => _incomingChannel.Writer.WriteAsync(message, cancellationToken).AsTask();

    private static Channel<T> CreateChannel<T>()
    {
        return Channel.CreateBounded<T>(new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = false });
    }

    public ValueTask DisposeAsync()
    {
        _incomingChannel.Writer.TryComplete();
        _outgoingChannel.Writer.TryComplete();

        return new ValueTask(_writeTask);
    }
}