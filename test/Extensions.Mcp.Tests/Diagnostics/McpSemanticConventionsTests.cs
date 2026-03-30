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

    [Fact]
    public void McpResourceUri_HasCorrectValue()
    {
        Assert.Equal("mcp.resource.uri", SemanticConventions.Mcp.ResourceUri);
    }

    // GenAI Attributes
    [Fact]
    public void GenAiToolName_HasCorrectValue()
    {
        Assert.Equal("gen_ai.tool.name", SemanticConventions.GenAi.ToolName);
    }

    [Fact]
    public void GenAiPromptName_HasCorrectValue()
    {
        Assert.Equal("gen_ai.prompt.name", SemanticConventions.GenAi.PromptName);
    }

    [Fact]
    public void GenAiOperationName_HasCorrectValue()
    {
        Assert.Equal("gen_ai.operation.name", SemanticConventions.GenAi.OperationName);
    }

    // JSON-RPC Attributes
    [Fact]
    public void JsonRpcRequestId_HasCorrectValue()
    {
        Assert.Equal("jsonrpc.request.id", SemanticConventions.JsonRpc.RequestId);
    }

    [Fact]
    public void JsonRpcProtocolVersion_HasCorrectValue()
    {
        Assert.Equal("jsonrpc.protocol.version", SemanticConventions.JsonRpc.ProtocolVersion);
    }

    // RPC Attributes
    [Fact]
    public void RpcResponseStatusCode_HasCorrectValue()
    {
        Assert.Equal("rpc.response.status_code", SemanticConventions.Rpc.ResponseStatusCode);
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
    public void MethodToolsCall_HasCorrectValue()
    {
        Assert.Equal("tools/call", SemanticConventions.Methods.ToolsCall);
    }

    [Fact]
    public void MethodToolsList_HasCorrectValue()
    {
        Assert.Equal("tools/list", SemanticConventions.Methods.ToolsList);
    }

    [Fact]
    public void MethodResourcesRead_HasCorrectValue()
    {
        Assert.Equal("resources/read", SemanticConventions.Methods.ResourcesRead);
    }

    [Fact]
    public void MethodResourcesList_HasCorrectValue()
    {
        Assert.Equal("resources/list", SemanticConventions.Methods.ResourcesList);
    }

    [Fact]
    public void MethodPromptsGet_HasCorrectValue()
    {
        Assert.Equal("prompts/get", SemanticConventions.Methods.PromptsGet);
    }

    [Fact]
    public void MethodPromptsList_HasCorrectValue()
    {
        Assert.Equal("prompts/list", SemanticConventions.Methods.PromptsList);
    }

    [Fact]
    public void MethodInitialize_HasCorrectValue()
    {
        Assert.Equal("initialize", SemanticConventions.Methods.Initialize);
    }

    // Operation Names
    [Fact]
    public void OperationExecuteTool_HasCorrectValue()
    {
        Assert.Equal("execute_tool", SemanticConventions.Operations.ExecuteTool);
    }

    // Error Type Values
    [Fact]
    public void ErrorTypeToolError_HasCorrectValue()
    {
        Assert.Equal("tool_error", SemanticConventions.Error.ToolError);
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
