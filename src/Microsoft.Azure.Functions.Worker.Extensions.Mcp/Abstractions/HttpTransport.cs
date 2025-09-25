// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents the an MCP HTTP transport, this could either be streamable or SSE.
/// </summary>
/// <remarks>Use this class to configure and manage HTTP-specific transport settings, such as custom headers, when
/// sending or receiving data over HTTP. This class is typically used as part of a larger communication framework that
/// supports multiple transport types.</remarks>
public class HttpTransport : Transport
{
    public HttpTransport(string name) => Name = name;

    /// <summary>
    /// Gets the HTTP transport type represented by the current instance.
    /// </summary>
    /// <remarks>The returned value indicates the protocol used for HTTP communication, such as streamable
    /// HTTP or server-sent events. If the transport type is not recognized, the value is <see
    /// cref="HttpTransportType.Unknown"/>.</remarks>
    public HttpTransportType Type => Name switch
    {
        "http-streamable" => HttpTransportType.Streamable,
        "http-sse" => HttpTransportType.ServerSentEvents,
        _ => HttpTransportType.Unknown,
    };

    /// <summary>
    /// Gets the collection of HTTP headers for the request.
    /// </summary>
    /// <remarks>Header names are compared using case-insensitive ordinal comparison.</remarks>
    public Dictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
