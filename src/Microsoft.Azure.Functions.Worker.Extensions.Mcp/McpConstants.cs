// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Constants used throughout the MCP extension
/// </summary>
internal static class McpConstants
{
    /// <summary>
    /// The SSE endpoint path
    /// </summary>
    internal const string SseEndpoint = "sse";
    
    /// <summary>
    /// The message endpoint path
    /// </summary>
    internal const string MessageEndpoint = "message";
    
    /// <summary>
    /// Query parameter name for Azure MCP state
    /// </summary>
    internal const string AzmcpStateQuery = "azmcpcs";
    
    /// <summary>
    /// Query parameter name for Functions code/key
    /// </summary>
    internal const string FunctionsCodeQuery = "code";
    
    /// <summary>
    /// Header name for Functions key
    /// </summary>
    internal const string FunctionsKeyHeader = "x-functions-key";
    
    /// <summary>
    /// Header name for MCP session ID
    /// </summary>
    internal const string McpSessionIdHeaderName = "MCP-Session-Id";
}
