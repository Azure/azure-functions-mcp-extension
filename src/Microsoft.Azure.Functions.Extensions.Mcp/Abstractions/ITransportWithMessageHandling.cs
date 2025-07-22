// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface ITransportWithMessageHandling : ITransport
{
    Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);
}
