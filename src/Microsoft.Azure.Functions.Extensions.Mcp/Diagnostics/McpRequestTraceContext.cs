// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Contains context information for MCP request tracing.
/// </summary>
internal readonly struct McpRequestTraceContext
{
    /// <summary>
    /// The MCP session ID.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// The client IP address.
    /// </summary>
    public string? ClientAddress { get; init; }

    /// <summary>
    /// The client port.
    /// </summary>
    public int? ClientPort { get; init; }

    /// <summary>
    /// The HTTP protocol version (e.g., "1.1", "2").
    /// </summary>
    public string? HttpProtocolVersion { get; init; }

    /// <summary>
    /// The MCP protocol version.
    /// </summary>
    public string? McpProtocolVersion { get; init; }

    /// <summary>
    /// Extracts request context from an HttpContext.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="sessionId">The MCP session ID.</param>
    /// <param name="mcpProtocolVersion">The MCP protocol version, if known.</param>
    /// <returns>A populated McpRequestTraceContext.</returns>
    public static McpRequestTraceContext FromHttpContext(
        HttpContext? httpContext,
        string? sessionId,
        string? mcpProtocolVersion = null)
    {
        if (httpContext is null)
        {
            return new McpRequestTraceContext
            {
                SessionId = sessionId,
                McpProtocolVersion = mcpProtocolVersion
            };
        }

        var connection = httpContext.Connection;
        var clientAddress = connection.RemoteIpAddress?.ToString();
        var clientPort = connection.RemotePort;

        // Extract HTTP version from request protocol (e.g., "HTTP/1.1" -> "1.1")
        var httpProtocolVersion = ExtractHttpVersion(httpContext.Request.Protocol);

        return new McpRequestTraceContext
        {
            SessionId = sessionId,
            ClientAddress = clientAddress,
            ClientPort = clientPort > 0 ? clientPort : null,
            HttpProtocolVersion = httpProtocolVersion,
            McpProtocolVersion = mcpProtocolVersion
        };
    }

    /// <summary>
    /// Extracts request context using a cached IHttpContextAccessor.
    /// </summary>
    /// <param name="accessor">The cached HTTP context accessor.</param>
    /// <param name="sessionId">The MCP session ID.</param>
    /// <param name="mcpProtocolVersion">The MCP protocol version, if known.</param>
    /// <returns>A populated McpRequestTraceContext.</returns>
    public static McpRequestTraceContext FromHttpContextAccessor(
        IHttpContextAccessor? accessor,
        string? sessionId,
        string? mcpProtocolVersion = null)
    {
        return FromHttpContext(accessor?.HttpContext, sessionId, mcpProtocolVersion);
    }

    /// <summary>
    /// Extracts the version number from an HTTP protocol string.
    /// </summary>
    /// <param name="protocol">The protocol string (e.g., "HTTP/1.1", "HTTP/2").</param>
    /// <returns>The version string (e.g., "1.1", "2").</returns>
    private static string? ExtractHttpVersion(string? protocol)
    {
        if (string.IsNullOrEmpty(protocol))
        {
            return null;
        }

        // Protocol format is typically "HTTP/1.1" or "HTTP/2"
        var slashIndex = protocol.IndexOf('/');
        if (slashIndex >= 0 && slashIndex < protocol.Length - 1)
        {
            return protocol[(slashIndex + 1)..];
        }

        return protocol;
    }
}
