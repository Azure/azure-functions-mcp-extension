// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Constants for MCP instrumentation following OpenTelemetry MCP semantic conventions.
/// See: https://opentelemetry.io/docs/specs/semconv/gen-ai/mcp/
/// </summary>
internal static class TraceConstants
{
    public const string ExtensionActivitySource = "Azure.Functions.Extensions.Mcp";
    public const string ExtensionActivitySourceVersion = "1.0.0.0";

    // Exception event attributes (OTel standard)
    public const string ExceptionEventNameAttribute = "exception";
    public const string ExceptionTypeAttribute = "exception.type";
    public const string ExceptionMessageAttribute = "exception.message";
    public const string ExceptionStacktraceAttribute = "exception.stacktrace";

    /// <summary>
    /// MCP semantic convention attribute names.
    /// </summary>
    public static class McpAttributes
    {
        // Required
        /// <summary>The name of the request or notification method (e.g., "tools/call", "resources/read").</summary>
        public const string MethodName = "mcp.method.name";

        // Conditionally Required
        /// <summary>Describes a class of error the operation ended with.</summary>
        public const string ErrorType = "error.type";

        /// <summary>Name of the tool utilized by the agent.</summary>
        public const string ToolName = "gen_ai.tool.name";

        /// <summary>Name of the prompt or prompt template.</summary>
        public const string PromptName = "gen_ai.prompt.name";

        /// <summary>A string representation of the JSON-RPC request id.</summary>
        public const string JsonRpcRequestId = "jsonrpc.request.id";

        /// <summary>The value of the resource URI.</summary>
        public const string ResourceUri = "mcp.resource.uri";

        /// <summary>The MIME type of the resource content.</summary>
        public const string ResourceMimeType = "mcp.resource.mime_type";

        /// <summary>The error code from the JSON-RPC response.</summary>
        public const string RpcResponseStatusCode = "rpc.response.status_code";

        // Recommended
        /// <summary>The name of the GenAI operation being performed (e.g., "execute_tool").</summary>
        public const string OperationName = "gen_ai.operation.name";

        /// <summary>Protocol version as specified in the jsonrpc property.</summary>
        public const string JsonRpcProtocolVersion = "jsonrpc.protocol.version";

        /// <summary>The version of the Model Context Protocol used.</summary>
        public const string ProtocolVersion = "mcp.protocol.version";

        /// <summary>Identifies the MCP session.</summary>
        public const string SessionId = "mcp.session.id";

        /// <summary>OSI application layer or non-OSI equivalent (e.g., "http", "websocket").</summary>
        public const string NetworkProtocolName = "network.protocol.name";

        /// <summary>The actual version of the protocol used for network communication.</summary>
        public const string NetworkProtocolVersion = "network.protocol.version";

        /// <summary>The transport protocol used for the MCP session (e.g., "tcp", "pipe").</summary>
        public const string NetworkTransport = "network.transport";

        /// <summary>Client address - domain name or IP address.</summary>
        public const string ClientAddress = "client.address";

        /// <summary>Client port number.</summary>
        public const string ClientPort = "client.port";

        // Opt-In
        /// <summary>Parameters passed to the tool call.</summary>
        public const string ToolCallArguments = "gen_ai.tool.call.arguments";

        /// <summary>The result returned by the tool call.</summary>
        public const string ToolCallResult = "gen_ai.tool.call.result";
    }

    /// <summary>
    /// Well-known MCP method names.
    /// </summary>
    public static class McpMethods
    {
        public const string Initialize = "initialize";
        public const string ToolsCall = "tools/call";
        public const string ToolsList = "tools/list";
        public const string ResourcesRead = "resources/read";
        public const string ResourcesList = "resources/list";
        public const string ResourcesSubscribe = "resources/subscribe";
        public const string ResourcesUnsubscribe = "resources/unsubscribe";
        public const string PromptsGet = "prompts/get";
        public const string PromptsList = "prompts/list";
        public const string Ping = "ping";
    }

    /// <summary>
    /// Well-known GenAI operation names.
    /// </summary>
    public static class GenAiOperations
    {
        public const string ExecuteTool = "execute_tool";
    }

    /// <summary>
    /// Well-known network transport values.
    /// </summary>
    public static class NetworkTransports
    {
        public const string Tcp = "tcp";
        public const string Pipe = "pipe";
        public const string Quic = "quic";
        public const string Unix = "unix";
    }
}
