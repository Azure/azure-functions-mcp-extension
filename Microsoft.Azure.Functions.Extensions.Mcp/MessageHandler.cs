using System.Buffers;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class MessageHandler(Stream eventStream) : IAsyncDisposable
{
    private readonly Channel<string> _incomingChannel = CreateChannel<string>();
    private readonly Channel<SseItem<string>> _outgoingChannel = CreateChannel<SseItem<string>>();
    private Task _writeTask = Task.CompletedTask;

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task Start(CancellationToken cancellationToken)
    {
        _outgoingChannel.Writer.TryWrite(new SseItem<string>("write the endpoint"));

        var events = _outgoingChannel.Reader.ReadAllAsync(cancellationToken);

        return _writeTask = SseFormatter.WriteAsync(events, eventStream, BufferWriter, cancellationToken);
    }

    private static void BufferWriter<T>(SseItem<T> sseItem, IBufferWriter<byte> writer)
    {
        JsonSerializer.Serialize(new Utf8JsonWriter(writer), sseItem.Data, McpJsonSerializerOptions.DefaultOptions)
    }

    public ChannelReader<string> MessageReader => _incomingChannel.Reader;

    public Task SendMessageAsync(string message, CancellationToken cancellationToken)
        => _outgoingChannel.Writer.WriteAsync(new SseItem<string>(message), cancellationToken).AsTask();

    public Task ProcessMessage(string message, CancellationToken cancellationToken)
        => _incomingChannel.Writer.WriteAsync(message, cancellationToken).AsTask();

    private static Channel<T> CreateChannel<T>()
    {
        return Channel.CreateBounded<T>(new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = false });
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        _incomingChannel.Writer.TryComplete();
        _outgoingChannel.Writer.TryComplete();

        return new ValueTask(_writeTask);
    }
}