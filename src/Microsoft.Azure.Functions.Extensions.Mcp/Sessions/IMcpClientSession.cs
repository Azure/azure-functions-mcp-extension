// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Server;
using System.Security.Cryptography.Xml;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpClientSession<TTransport> : IMcpClientSession where TTransport : ITransport
{
    TTransport Transport { get; }
}

internal interface IMcpClientSession : IAsyncDisposable
{
    string ClientId { get; }

    string InstanceId { get; }

    IMcpServer? Server { get; set; }
}
