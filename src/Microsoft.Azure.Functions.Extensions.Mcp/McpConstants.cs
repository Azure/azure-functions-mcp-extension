// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpConstants
{
    public const string AzmcpStateQuery = "azmcpcs";
    public const string FunctionsCodeQuery = "code";
    public const string FunctionsKeyHeader = "x-functions-key";
    public const string SseEndpoint = "sse";
    public const string MessageEndpoint = "message";
    public const string McpSessionIdHeaderName = "Mcp-Session-Id";
    public const string McpTransportName = "functions_mcp:transportname";

    internal sealed class ToolResultContentTypes
    {
        public const string CallToolResult = "call_tool_result";
        public const string MultiContentResult = "multi_content_result";
        public const string Audio = "audio";
        public const string Text = "text";
        public const string Image = "image";
        public const string Resource = "resource";
        public const string ResourceLink = "resource_link";
    }
}
