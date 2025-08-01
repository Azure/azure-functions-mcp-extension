// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpClientSessionManager
{
    IMcpClientSession<TTransport> CreateSession<TTransport>(string clientId, string instanceId, TTransport transport)
        where TTransport : ITransportWithMessageHandling;

    bool TryGetSession(string clientId, [NotNullWhen(true)] out IMcpClientSession? clientSession);
}
