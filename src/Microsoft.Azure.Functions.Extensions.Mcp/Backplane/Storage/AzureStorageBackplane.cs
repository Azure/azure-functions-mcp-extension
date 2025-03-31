using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using System.Text.Json;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;

internal class AzureStorageBackplane : IMcpBackplane, IAsyncDisposable
{
    private readonly Channel<McpBackplaneMessage> _mesageChannel = Channel.CreateUnbounded<McpBackplaneMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly IMcpInstanceIdProvider _instanceIdProvider;
    private readonly QueueServiceClientProvider _queueServiceProvider;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _processingCts;

    public AzureStorageBackplane(IMcpInstanceIdProvider instanceIdProvider, QueueServiceClientProvider queueServiceClientProvider)
    {
        _processingCts = new CancellationTokenSource();

        _instanceIdProvider = instanceIdProvider;
        _queueServiceProvider = queueServiceClientProvider;
        _processingTask = StartProcessingAsync();
    }

    public ChannelReader<McpBackplaneMessage> Messages => _mesageChannel.Reader;

    public async Task SendMessageAsync(IJsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        QueueClient queueClient = await GetQueueClientAsync(instanceId, cancellationToken);
        var backplaneMessage = new McpBackplaneMessage { ClientId = clientId, Message = message };

        await queueClient.SendMessageAsync(JsonSerializer.Serialize(backplaneMessage, McpJsonSerializerOptions.DefaultOptions), cancellationToken);
    }

    private async Task StartProcessingAsync()
    {
        var client = await GetQueueClientAsync(_instanceIdProvider.InstanceId, _processingCts.Token);
        while (!_processingCts.Token.IsCancellationRequested)
        {
            try
            {
                var messages = await client.ReceiveMessagesAsync(cancellationToken: _processingCts.Token);

                foreach (var message in messages.Value)
                {
                    var backplaneMessage = JsonSerializer.Deserialize<McpBackplaneMessage>(message.MessageText, McpJsonSerializerOptions.DefaultOptions);
                    if (backplaneMessage != null)
                    {
                        await _mesageChannel.Writer.WriteAsync(backplaneMessage, _processingCts.Token);
                    }

                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, _processingCts.Token);
                }

                await Task.Delay(1000, _processingCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        await client.DeleteIfExistsAsync();
    }

    private async Task<QueueClient> GetQueueClientAsync(string instanceId, CancellationToken cancellationToken)
    {
        var serviceClient = _queueServiceProvider.Get();
        var queueClient = serviceClient.GetQueueClient($"mcp-backplane-{instanceId}");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        return queueClient;
    }

    public ValueTask DisposeAsync()
    {
        _mesageChannel.Writer.TryComplete();
        _processingCts.Cancel();

        return new ValueTask(_processingTask);
    }
}