// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging;
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
    private readonly string _instanceId;
    private readonly IMcpBackplane _backplane;
    private readonly ILogger<Logs.DefaultMessageHandler> _logger;
    private readonly Task _backplaneProcessingTask;
    private readonly McpOptions _mcpOptions;
    private readonly RequestActivityFactory _requestActivityFactory;

    public DefaultMessageHandlerManager(IToolRegistry toolRegistry, IMcpInstanceIdProvider instanceIdProvider, IMcpBackplane backplane, IOptions<McpOptions> mcpOptions, 
        ILogger<Logs.DefaultMessageHandler> logger, RequestActivityFactory activityFactory )
    {
        _toolRegistry = toolRegistry;
        _instanceId = instanceIdProvider.InstanceId;
        _backplane = backplane;
        _logger = logger;
        _backplaneProcessingTask = InitializeBackplaneProcessing(_backplane.Messages);
        _mcpOptions = mcpOptions.Value;
        _requestActivityFactory = activityFactory;
    }

    private async Task InitializeBackplaneProcessing(ChannelReader<McpBackplaneMessage> messages)
    {
        try
        {
            _logger.LogInformation("Initializing backplane message processing.");

            await foreach (var message in messages.ReadAllAsync())
            {
                _ = HandleMessageAsync(message.ClientId, message.Message, CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (messages.Completion.IsCompleted)
        {
            _logger.LogInformation("Backplane message processing completed.");
        }
    }

    public IMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken)
    {
        var clientId = Utility.CreateId();
        _logger.LogInformation("Creating message handler for client '{clientId}'", clientId);

        var handler = new MessageHandler(eventStream, clientId);

        var processingTask = ProcessMessagesAsync(handler, cancellationToken);

        var handlerReference = new MessageHandlerReference(handler, processingTask);
        _handlers.TryAdd(handler.Id, handlerReference);

        _logger.LogInformation("Message handler created with ID: '{clientId}' on instance '{instanceId}'", handler.Id, _instanceId);

        return handler;
    }

    public Task CloseHandlerAsync(IMessageHandler handler)
    {
        _logger.LogInformation("Closing message handler with ID: {clientId}", handler.Id);

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
        try
        {
            if (string.Equals(instanceId, _instanceId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Handling message for client '{clientId}'", clientId);

                await HandleMessageAsync(clientId, message, cancellationToken);
            }
            else
            {
                _logger.LogDebug("Sending message to backplane for instance '{instanceId}', client '{clientId}'", instanceId, clientId);

                // Route this request to the appropriate instance
                await _backplane.SendMessageAsync(message, instanceId, clientId, cancellationToken);
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error handling message for client '{clientId}'", clientId);
            throw;
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
            throw new InvalidOperationException($"Invalid client id. The client '{clientId}' does not exist in instance '{_instanceId}'.");
        }

        return handler.ProcessMessageAsync(message, cancellationToken);
    }

    private async Task ProcessMessagesAsync(IMessageHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting message processing loop for handler '{handlerId}'.", handler.Id);

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
            _logger.LogDebug("Message processing loop for handler '{handlerId}' completed.", handler.Id);

            _handlers.TryRemove(handler.Id, out _);
        }
    }

    private async Task ProcessMessageAsync(IMessageHandler handler, JsonRpcMessage message, CancellationToken cancellationToken)
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
                }
                catch (OperationCanceledException)
                {
                    // Cancellation. Add logging.
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "Error processing request: '{requestId}' for client '{handlerId}'", request.Id, handler.Id);

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
                        InputSchema = t.GetPropertiesInputSchema()
                    }).ToList();

                return JsonSerializer.SerializeToNode(new ListToolsResult {Tools = tools}, GetTypeInfo<ListToolsResult>());
            case RequestMethods.ToolsCall:
                var typedRequest = request.Params.Deserialize(GetTypeInfo<CallToolRequestParams>());

                if (typedRequest is not null
                    && _toolRegistry.TryGetTool(typedRequest.Name, out var tool))
                {
                    using var activity = _requestActivityFactory.CreateActivity(tool.Name, request);

                    try
                    {
                        var result = await tool.RunAsync(typedRequest, cancellationToken);

                        return JsonSerializer.SerializeToNode(result, GetTypeInfo<CallToolResponse>());
                    }
                    catch (Exception exc)
                    {
                        activity?.SetExceptionStatus(null, DateTimeOffset.UtcNow);

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

    private record MessageHandlerReference(IMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;

        public Task MessageProcessingTask { get; init; } = MessageProcessingTask;
    }
}
