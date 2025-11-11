// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpClientSession<TTransport> : IMcpClientSession where TTransport : IMcpExtensionTransport
{
    TTransport Transport { get; }
}

internal interface IMcpClientSession : IAsyncDisposable
{
    string ClientId { get; }

    string InstanceId { get; }

    McpServer? Server { get; set; }

    Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);

    Task StartPingAsync(CancellationToken cancellationToken);

    Task StopPingAsync();
}
