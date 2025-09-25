// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Specifies the type of HTTP transport to be used for MCP communication.
/// </summary>
public enum HttpTransportType
{
    Streamable,
    ServerSentEvents,
    Unknown
}
