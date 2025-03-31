using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IMessageHandlerManager
{
    IMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken);

    Task CloseHandlerAsync(IMessageHandler handler);

    bool TryGetHandler(string id, [NotNullWhen(true)] out IMessageHandler? messageHandler);

    Task HandleMessageAsync(IJsonRpcMessage message, string instanceId, string mcpClientId, CancellationToken requestAborted);
}