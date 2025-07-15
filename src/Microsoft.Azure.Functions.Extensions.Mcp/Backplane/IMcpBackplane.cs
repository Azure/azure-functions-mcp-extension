// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Channels;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane;

public interface IMcpBackplane
{
    ChannelReader<McpBackplaneMessage> Messages { get; }

    Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken);
}
