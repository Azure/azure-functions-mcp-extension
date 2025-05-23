// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Channels;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IMessageHandler
{
    string Id { get; }

    ChannelReader<JsonRpcMessage> MessageReader { get; }

    Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);

    Task ProcessMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);

    Task RunAsync(Func<string, string> endpointWriter, CancellationToken cancellationToken);
}