// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// OpenTelemetry Semantic Conventions for MCP (Model Context Protocol).
/// Based on: https://opentelemetry.io/docs/specs/semconv/gen-ai/mcp/
/// </summary>
internal static class SemanticConventions
{
    public static class Mcp
    {
        public const string MethodName = "mcp.method.name";
        public const string SessionId = "mcp.session.id";
        public const string ProtocolVersion = "mcp.protocol.version";
    }

    public static class JsonRpc
    {
        public const string ProtocolVersion = "jsonrpc.protocol.version";
        public const string Version = "2.0";
    }

    public static class Error
    {
        public const string Type = "error.type";
    }

    public static class Exception
    {
        public const string EventName = "exception";
        public const string Type = "exception.type";
        public const string Message = "exception.message";
        public const string Stacktrace = "exception.stacktrace";
    }

    public static class Client
    {
        public const string Address = "client.address";
        public const string Port = "client.port";
    }

    public static class Network
    {
        public const string ProtocolName = "network.protocol.name";
        public const string ProtocolVersion = "network.protocol.version";
        public const string Transport = "network.transport";
        public const string TransportTcp = "tcp";
        public const string ProtocolHttp = "http";
    }

    public static class Methods
    {
        public const string Initialize = "initialize";
        public const string SessionDelete = "session/delete";
    }
}
