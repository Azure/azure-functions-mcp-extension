using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Utils.Json;

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

    public async Task HandleMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
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

    private Task HandleMessageAsync(string clientId, JsonRpcMessage message, CancellationToken cancellationToken)
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

    private async Task ProcessMessageAsync(IMessageHandler handler, JsonRpcMessage message,
        CancellationToken cancellationToken)
    {
        switch (message)
        {
            case JsonRpcRequest request:

                try
                {
                    var result = await HandleRequestAsync(request, cancellationToken);
                    var response = new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = result
                    };

                    await handler.SendMessageAsync(response, cancellationToken);
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Cancellation. Add logging.
                }
                catch (Exception exc)
                {
                    var detail = exc is McpException mcpException
                        ? new JsonRpcErrorDetail
                        {
                            Code = (int) mcpException.ErrorCode,
                            Message = mcpException.Message
                        }
                        : new JsonRpcErrorDetail
                        {
                            Code = (int) McpErrorCode.InternalError,
                            Message = exc.Message,
                        };

                    var error = new JsonRpcError
                    {
                        Id = request.Id,
                        JsonRpc = "2.0",
                        Error = detail
                    };

                    await handler.SendMessageAsync(error, cancellationToken);
                }

                break;
        }
    }

    private JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
               ?? throw new InvalidOperationException($"Unable to get type info for {typeof(T)}.");
    }

    private async Task<JsonNode?> HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        switch (request.Method)
        {
            case RequestMethods.ToolsList:
                var tools = _toolRegistry.GetTools()
                    .Select(t => new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = GetPropertiesInputSchema(t)
                    }).ToList();

                return JsonSerializer.SerializeToNode(new ListToolsResult {Tools = tools}, GetTypeInfo<ListToolsResult>());
            case RequestMethods.ToolsCall:
                var typedRequest = request.Params.Deserialize(GetTypeInfo<CallToolRequestParams>());

                if (typedRequest is not null
                    && _toolRegistry.TryGetTool(typedRequest.Name, out var tool))
                {
                    try
                    {
                        var result = await tool.RunAsync(typedRequest, cancellationToken);

                        return JsonSerializer.SerializeToNode(result, GetTypeInfo<CallToolResponse>());
                    }
                    catch (Exception exc)
                    {
                        throw new McpException("Method not found.", exc, McpErrorCode.MethodNotFound);
                    }
                }

                break;
            case RequestMethods.Initialize:
                var initializeResult = new InitializeResult
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

                return JsonSerializer.SerializeToNode(initializeResult, GetTypeInfo<InitializeResult>());
        }

        throw new McpException("Method not found.", McpErrorCode.MethodNotFound);
    }

    private JsonElement GetPropertiesInputSchema(IMcpTool tool)
    {
        var schema = new
        {
            type = "object",
            properties = tool.Properties.ToDictionary(
                prop => prop.PropertyName,
                prop => new
                {
                    type = prop.PropertyType,
                    description = prop.Description ?? string.Empty
                }
            ),
            required = tool.Properties.Select(prop => prop.PropertyName).ToArray()
        };

        var jsonString = JsonSerializer.Serialize(schema);
        using var document = JsonDocument.Parse(jsonString);
        return document.RootElement.Clone();
    }

    private record MessageHandlerReference(IMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;

        public Task MessageProcessingTask { get; init; } = MessageProcessingTask;
    }
}