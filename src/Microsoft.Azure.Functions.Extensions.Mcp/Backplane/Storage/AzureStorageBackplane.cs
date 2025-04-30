using System.Text.Json;
using System.Threading.Channels;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;

internal class AzureStorageBackplane : IMcpBackplane, IAsyncDisposable
{
    private readonly Channel<McpBackplaneMessage> _messageChannel = Channel.CreateUnbounded<McpBackplaneMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly IMcpInstanceIdProvider _instanceIdProvider;
    private readonly QueueServiceClientProvider _queueServiceProvider;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _processingCts;
    private readonly ILogger<AzureStorageBackplane> _logger;

    public AzureStorageBackplane(IMcpInstanceIdProvider instanceIdProvider, QueueServiceClientProvider queueServiceClientProvider, ILogger<AzureStorageBackplane> logger)
    {
        _processingCts = new CancellationTokenSource();

        _logger = logger;
        _instanceIdProvider = instanceIdProvider;
        _queueServiceProvider = queueServiceClientProvider;
        _processingTask = StartProcessingAsync();
    }

    public ChannelReader<McpBackplaneMessage> Messages => _messageChannel.Reader;

    public async Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        QueueClient queueClient = await GetQueueClientAsync(instanceId, cancellationToken);
        var backplaneMessage = new McpBackplaneMessage { ClientId = clientId, Message = message };

        await queueClient.SendMessageAsync(JsonSerializer.Serialize(backplaneMessage, McpJsonSerializerOptions.DefaultOptions), cancellationToken);
    }

    private async Task StartProcessingAsync()
    {
        var client = await GetQueueClientAsync(_instanceIdProvider.InstanceId, _processingCts.Token);

        const int maxDelay = 1500;
        int idleCount = 0;
        while (!_processingCts.Token.IsCancellationRequested)
        {
            try
            {
                var messages = await client.ReceiveMessagesAsync(cancellationToken: _processingCts.Token);

                if (messages.Value.Length == 0)
                {
                    idleCount++;
                    int delay = Math.Min(maxDelay, 100 * idleCount);
                    await Task.Delay(delay, _processingCts.Token);

                    continue;
                }

                // Reset idle count
                idleCount = 0;

                var processingTasks = messages.Value.Select(message => ProcessMessageAsync(message, client));

                await Task.WhenAll(processingTasks);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        await client.DeleteIfExistsAsync();
    }

    private async Task ProcessMessageAsync(QueueMessage message, QueueClient client)
    {
        try
        {
            var backplaneMessage = message.Body.ToObjectFromJson<McpBackplaneMessage>(McpJsonSerializerOptions.DefaultOptions);
            if (backplaneMessage != null)
            {
                await _messageChannel.Writer.WriteAsync(backplaneMessage, _processingCts.Token);
            }

            await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, _processingCts.Token);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to deserialize backplane message from queue {QueueName}, messageId={MessageId}", client.Name, message.MessageId);

            await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, _processingCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing backplane message from queue {QueueName}, messageId={MessageId}", client.Name, message.MessageId);
        }
    }

    private async Task<QueueClient> GetQueueClientAsync(string instanceId, CancellationToken cancellationToken)
    {
        var serviceClient = _queueServiceProvider.Get();
        var queueClient = serviceClient.GetQueueClient($"mcp-backplane-{instanceId}");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        return queueClient;
    }

    public async ValueTask DisposeAsync()
    {
        _messageChannel.Writer.TryComplete();
        await _processingCts.CancelAsync();

        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }

        _processingCts.Dispose();
    }
}