// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpExtensionTransport : ITransport
{
    bool IsStateless { get; set; }

    SessionContext? SessionContext { get; set; }

    Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);
}
