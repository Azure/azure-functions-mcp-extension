using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class DefaultMcpMessageHandlerManager(IToolRegistry toolRegistry) : IMcpMessageHandlerManager
#pragma warning restore CS9113 // Parameter is unread.
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
                _ = ProcessMessageAsync(handler, message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation
        }
    }

    private async Task ProcessMessageAsync(IMcpMessageHandler handler, IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        // switch on message type here
        // Add logging
        switch (message)
        {
            case JsonRpcRequest request:
                var result = await HandleRequestAsync(request, cancellationToken);
                var response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result,
                    
                };
                await handler.SendMessageAsync(response, cancellationToken);
                break;
        }
    }

    private async Task<object> HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        switch (request.Method)
        {
            case "tools/list":
                var tools = new ListToolsResult
                {
                    Tools =
                    [
                        new Tool
                        {
                            Name = "Foo",
                            Description = "Foo tool",
                        }
                    ]
                };

                return await Task.FromResult<ListToolsResult>(tools);
            case "tools/call":
                break;
            case "initialize":
                return Task.FromResult(new InitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    ServerInfo = new Implementation
                    {
                        Name = "Azure Functions MCP server.",
                        Version = typeof(DefaultMcpMessageHandlerManager).Assembly.GetName().Version?.ToString() ??
                                  string.Empty
                    },
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability
                        {
                            ListChanged = false
                        }
                    }
                });

        }

        return new JsonRpcError
        {
            Id = RequestId.FromString("test"),
            Error = new JsonRpcErrorDetail()
            {
                Code = -32601,
                Message = "Method not found",
            }
        };
    }

    private record MessageHandlerReference(IMcpMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;
    }
}