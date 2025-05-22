// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IMessageHandlerManager
{
    IMessageHandler CreateHandler(Stream eventStream, CancellationToken cancellationToken);

    Task CloseHandlerAsync(IMessageHandler handler);

    bool TryGetHandler(string id, [NotNullWhen(true)] out IMessageHandler? messageHandler);

    Task HandleMessageAsync(JsonRpcMessage message, string instanceId, string mcpClientId, CancellationToken requestAborted);
}
