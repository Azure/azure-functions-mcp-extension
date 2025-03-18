using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultMcpMessageHandlerManager(IToolRegistry toolRegistry) : IMcpMessageHandlerManager
{
    private readonly Dictionary<string, MessageHandlerReference> _handlers = new();

    public IMcpMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken)
    {
        var handler = new MessageHandler(eventStream);
        var processingTask = ProcessMessagesAsync(handler, cancellationToken);

        var handlerReference = new MessageHandlerReference(handler, processingTask);
        _handlers.Add(handler.Id, handlerReference);

        return handler;
    }

    public Task CloseHandlerAsync(IMcpMessageHandler handler)
    {
        return (handler as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }

    public bool TryGetHandler(string id, [NotNullWhen(true)] out IMcpMessageHandler? messageHandler)
    {
        messageHandler = null;

        if (_handlers.TryGetValue(id, out var reference))
        {
            messageHandler = reference.Handler;
        }

        return messageHandler is not null;
    }

    private async Task ProcessMessagesAsync(IMcpMessageHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in handler.MessageReader.ReadAllAsync(cancellationToken))
            {
                _ = ProcessMessageAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation
        }
    }

    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        // switch on message type here
        // Add logging
        switch (message)
        {
            case "start":
                if (toolRegistry.TryGetTool("foo", out var tool))
                {
                    await tool.RunAsync(cancellationToken);
                }
                break;
        }
    }

    private record MessageHandlerReference(IMcpMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;
    }
}