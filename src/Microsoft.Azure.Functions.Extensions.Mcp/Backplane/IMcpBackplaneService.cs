// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane
{
    internal interface IMcpBackplaneService
    {
        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

        Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken);
    }
}
