// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class McpSemanticConventionsTests
{
    [Fact]
    public void ActivitySourceName_HasCorrectValue()
    {
        Assert.Equal("Azure.Functions.Extensions.Mcp", McpDiagnosticsConstants.ActivitySourceName);
    }

    [Fact]
    public void ActivitySourceVersion_HasCorrectValue()
    {
        Assert.Equal("2.0.0", McpDiagnosticsConstants.ActivitySourceVersion);
    }

    // MCP Attributes
    [Fact]
    public void McpMethodName_HasCorrectValue()
    {
        Assert.Equal("mcp.method.name", SemanticConventions.Mcp.MethodName);
    }

    [Fact]
    public void McpSessionId_HasCorrectValue()
    {
        Assert.Equal("mcp.session.id", SemanticConventions.Mcp.SessionId);
    }

    [Fact]
    public void McpProtocolVersion_HasCorrectValue()
    {
        Assert.Equal("mcp.protocol.version", SemanticConventions.Mcp.ProtocolVersion);
    }

    // JSON-RPC Attributes
    [Fact]
    public void JsonRpcProtocolVersion_HasCorrectValue()
    {
        Assert.Equal("jsonrpc.protocol.version", SemanticConventions.JsonRpc.ProtocolVersion);
    }

    // Error Attributes
    [Fact]
    public void ErrorType_HasCorrectValue()
    {
        Assert.Equal("error.type", SemanticConventions.Error.Type);
    }

    // Exception Attributes
    [Fact]
    public void ExceptionEventName_HasCorrectValue()
    {
        Assert.Equal("exception", SemanticConventions.Exception.EventName);
    }

    [Fact]
    public void ExceptionType_HasCorrectValue()
    {
        Assert.Equal("exception.type", SemanticConventions.Exception.Type);
    }

    [Fact]
    public void ExceptionMessage_HasCorrectValue()
    {
        Assert.Equal("exception.message", SemanticConventions.Exception.Message);
    }

    [Fact]
    public void ExceptionStacktrace_HasCorrectValue()
    {
        Assert.Equal("exception.stacktrace", SemanticConventions.Exception.Stacktrace);
    }

    // Client Attributes
    [Fact]
    public void ClientAddress_HasCorrectValue()
    {
        Assert.Equal("client.address", SemanticConventions.Client.Address);
    }

    [Fact]
    public void ClientPort_HasCorrectValue()
    {
        Assert.Equal("client.port", SemanticConventions.Client.Port);
    }

    // Network Attributes
    [Fact]
    public void NetworkProtocolName_HasCorrectValue()
    {
        Assert.Equal("network.protocol.name", SemanticConventions.Network.ProtocolName);
    }

    [Fact]
    public void NetworkProtocolVersion_HasCorrectValue()
    {
        Assert.Equal("network.protocol.version", SemanticConventions.Network.ProtocolVersion);
    }

    [Fact]
    public void NetworkTransport_HasCorrectValue()
    {
        Assert.Equal("network.transport", SemanticConventions.Network.Transport);
    }

    // MCP Method Names
    [Fact]
    public void MethodInitialize_HasCorrectValue()
    {
        Assert.Equal("initialize", SemanticConventions.Methods.Initialize);
    }

    [Fact]
    public void MethodSessionDelete_HasCorrectValue()
    {
        Assert.Equal("session/delete", SemanticConventions.Methods.SessionDelete);
    }

    // Network Values
    [Fact]
    public void NetworkTransportTcp_HasCorrectValue()
    {
        Assert.Equal("tcp", SemanticConventions.Network.TransportTcp);
    }

    [Fact]
    public void NetworkProtocolHttp_HasCorrectValue()
    {
        Assert.Equal("http", SemanticConventions.Network.ProtocolHttp);
    }

    // JSON-RPC Values
    [Fact]
    public void JsonRpcVersion_HasCorrectValue()
    {
        Assert.Equal("2.0", SemanticConventions.JsonRpc.Version);
    }
}
