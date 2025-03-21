using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultMessageHandlerManager(IToolRegistry toolRegistry) : IMessageHandlerManager
{
    private readonly Dictionary<string, MessageHandlerReference> _handlers = new();

    public IMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken)
    {
        var handler = new MessageHandler(eventStream);
        var processingTask = ProcessMessagesAsync(handler, cancellationToken);

        var handlerReference = new MessageHandlerReference(handler, processingTask);
        _handlers.Add(handler.Id, handlerReference);

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
            _handlers.Remove(handler.Id);
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

                var tools = toolRegistry.GetTools()
                    .Select(t => new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = new JsonSchema
                        {
                            Properties = GetProperties(t)
                        }
                    }).ToList();

                return new ListToolsResult {Tools = tools};
            case "tools/call":
                if (request.Params is JsonElement paramsElement)
                {
                    var callToolRequest = paramsElement.Deserialize<ToolInvocationContext>();
                    if (callToolRequest is not null
                        && toolRegistry.TryGetTool(callToolRequest.Name, out var tool))
                    {
                        return await tool.RunAsync(callToolRequest, cancellationToken);
                    }
                }
                break;
            case "initialize":
                return new InitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    ServerInfo = new Implementation
                    {
                        Name = "Azure Functions MCP server.",
                        Version = typeof(DefaultMessageHandlerManager).Assembly.GetName().Version?.ToString() ??
                                  string.Empty
                    },
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability
                        {
                            ListChanged = false
                        }
                    }
                };

        }

        return new JsonRpcError
        {
            Id = RequestId.FromString("test"),
            Error = new JsonRpcErrorDetail()
            {
                Code = ErrorCodes.MethodNotFound,
                Message = "Method not found",
            }
        };
    }

    private Dictionary<string, JsonSchemaProperty> GetProperties(IMcpTool t)
    {
        return t.Properties.ToDictionary(
            p => p.Name,
            p => new JsonSchemaProperty
            {
                Type = p.PropertyType,
                Description = p.Description,
            });
    }

    private record MessageHandlerReference(IMessageHandler Handler, Task MessageProcessingTask)
    {
        public string Id => Handler.Id;
    }
}