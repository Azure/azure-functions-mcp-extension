using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpMessageHandlerManager
{
    IMcpMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken);

    Task CloseHandlerAsync(IMcpMessageHandler handler);

    bool TryGetHandler(string id, [NotNullWhen(true)] out IMcpMessageHandler? messageHandler);
}