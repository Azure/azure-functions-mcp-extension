using System.Collections.Concurrent;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using System.Threading;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultMessageHandlerManager : IMessageHandlerManager, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, MessageHandlerReference> _handlers = new();
    private readonly IToolRegistry _toolRegistry;
    private readonly IMcpInstanceIdProvider _instanceIdProvider;
    private readonly IMcpBackplane _backplane;
    private readonly Task _backplaneProcessingTask;
    private readonly McpOptions _mcpOptions;

    public DefaultMessageHandlerManager(IToolRegistry toolRegistry, IMcpInstanceIdProvider instanceIdProvider, IMcpBackplane backplane, IOptions<McpOptions> mcpOptions)
    {
        _toolRegistry = toolRegistry;
        _instanceIdProvider = instanceIdProvider;
        _backplane = backplane;
        _backplaneProcessingTask = InitializeBackplaneProcessing(_backplane.Messages);
        _mcpOptions = mcpOptions.Value;
    }

    private async Task InitializeBackplaneProcessing(ChannelReader<McpBackplaneMessage> messages)
    {
        try
        {
            await foreach (var message in messages.ReadAllAsync())
            {
                _ = HandleMessageAsync(message.ClientId, message.Message, CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (messages.Completion.IsCompleted)
        {
            // Ignore cancellation
        }
    }

    public IMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken)
    {
        var handler = new MessageHandler(eventStream);
        var processingTask = ProcessMessagesAsync(handler, cancellationToken);

        var handlerReference = new MessageHandlerReference(handler, processingTask);
        _handlers.TryAdd(handler.Id, handlerReference);

        return handler;
    }

    public Task CloseHandlerAsync(IMessageHandler handler)
    {
        return (handler as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }

    public bool TryGetHandler(string id, [NotNullWhen(true)] out IMessageHandler? messageHandler)
    {
        messageHandler = null;

        if (_handlers.TryGetValue(id, out var reference))
        {
            messageHandler = reference.Handler;
        }

        return messageHandler is not null;
    }

    public async Task HandleMessageAsync(IJsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
    {
        if (string.Equals(instanceId, _instanceIdProvider.InstanceId, StringComparison.OrdinalIgnoreCase))
        {
            await HandleMessageAsync(clientId, message, cancellationToken);
        }
        else
        {
            // Route this request to the appropriate instance
            await _backplane.SendMessageAsync(message, instanceId, clientId, cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(_backplaneProcessingTask);
    }

    private Task HandleMessageAsync(string clientId, IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        if (!TryGetHandler(clientId, out IMessageHandler? handler))
        {
            throw new InvalidOperationException("Invalid client id.");
        }

        return handler.ProcessMessageAsync(message, cancellationToken);
    }

    private async Task ProcessMessagesAsync(IMessageHandler handler, CancellationToken cancellationToken)
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
        finally
        {
            _handlers.TryRemove(handler.Id, out _);
        }
    }

    private async Task ProcessMessageAsync(IMessageHandler handler, IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case JsonRpcRequest request:
                var result = await HandleRequestAsync(request, cancellationToken);
                var response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
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
                var tools = _toolRegistry.GetTools()
                    .Select(t => new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = new JsonSchema
                        {
                            Properties = GetProperties(t)
                        }
                    }).ToList();

                return new ListToolsResult { Tools = tools };
            case "tools/call":
                if (request.Params is JsonElement paramsElement)
                {
                    var callToolRequest = paramsElement.Deserialize<ToolInvocationContext>();
                    if (callToolRequest is not null
                        && _toolRegistry.TryGetTool(callToolRequest.Name, out var tool))
                    {
                        try
                        {
                            return await tool.RunAsync(callToolRequest, cancellationToken);
                        }
                        catch (Exception)
                        {
                            return new JsonRpcError
                            {
                                Id = request.Id,
                                Error = new JsonRpcErrorDetail
                                {
                                    Code = ErrorCodes.InternalError,
                                    Message = "Tool invocation failure."
                                }
                            };
                        }
                    }
                }
                break;
            case "initialize":
                return new InitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    ServerInfo = new Implementation
                    {
                        Name = _mcpOptions.ServerName,
                        Version = _mcpOptions.ServerVersion
                    },
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability
                        {
                            ListChanged = false
                        }
                    },
                    Instructions = _mcpOptions.Instructions
                };
        }

        return new JsonRpcError
        {
            Id = request.Id,
            Error = new JsonRpcErrorDetail()
            {
                Code = ErrorCodes.MethodNotFound,
                Message = "Method not found",
            }
        };
    }

    private Dictionary<string, JsonSchemaProperty> GetProperties(IMcpTool tool)
    {
        return tool.Properties.ToDictionary(
            p => p.PropertyName,
            p => new JsonSchemaProperty
            {
                Type = p.PropertyType,
                Description = p.Description,
            });
    }

    private record MessageHandlerReference(IMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;

        public Task MessageProcessingTask { get; init; } = MessageProcessingTask;
    }
}