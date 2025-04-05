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
    private Task _handlerTasks = Task.CompletedTask;
    private long _currentRequestId = 0;

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task RunAsync(Func<string, string> endpointWriter, CancellationToken cancellationToken)
    {
        var endpointResponse = new JsonRpcResponse { Id = RequestId.FromString(Id), Result = endpointWriter(Id) };
        _outgoingChannel.Writer.TryWrite(new SseItem<IJsonRpcMessage>(endpointResponse, "endpoint"));

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

            var requestId = RequestId.FromString($"{Id}-{Interlocked.Increment(ref _currentRequestId)}");
            await SendMessageAsync(new JsonRpcRequest { Id = requestId, Method = "ping" }, cancellationToken);
        }
    }

    private static void BufferWriter(SseItem<IJsonRpcMessage> sseItem, IBufferWriter<byte> writer)
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

    public ChannelReader<IJsonRpcMessage> MessageReader => _incomingChannel.Reader;

    public Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
        => _outgoingChannel.Writer.WriteAsync(new SseItem<IJsonRpcMessage>(message), cancellationToken).AsTask();

    public Task ProcessMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
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

internal sealed record EndpointContext(string ClientId, string InstanceId, string? Code)
{
}