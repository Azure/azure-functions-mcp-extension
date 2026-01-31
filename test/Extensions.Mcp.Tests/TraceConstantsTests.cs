// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class TraceConstantsTests
{
    [Fact]
    public void McpAttributes_HasRequiredAttributeConstants()
    {
        // Required attributes
        Assert.Equal("mcp.method.name", TraceConstants.McpAttributes.MethodName);
    }

    [Fact]
    public void McpAttributes_HasConditionallyRequiredAttributeConstants()
    {
        // Conditionally required
        Assert.Equal("error.type", TraceConstants.McpAttributes.ErrorType);
        Assert.Equal("gen_ai.tool.name", TraceConstants.McpAttributes.ToolName);
        Assert.Equal("gen_ai.prompt.name", TraceConstants.McpAttributes.PromptName);
        Assert.Equal("jsonrpc.request.id", TraceConstants.McpAttributes.JsonRpcRequestId);
        Assert.Equal("mcp.resource.uri", TraceConstants.McpAttributes.ResourceUri);
        Assert.Equal("mcp.resource.mime_type", TraceConstants.McpAttributes.ResourceMimeType);
    }

    [Fact]
    public void McpAttributes_HasRecommendedAttributeConstants()
    {
        // Recommended
        Assert.Equal("gen_ai.operation.name", TraceConstants.McpAttributes.OperationName);
        Assert.Equal("jsonrpc.protocol.version", TraceConstants.McpAttributes.JsonRpcProtocolVersion);
        Assert.Equal("mcp.protocol.version", TraceConstants.McpAttributes.ProtocolVersion);
        Assert.Equal("mcp.session.id", TraceConstants.McpAttributes.SessionId);
    }

    [Fact]
    public void McpMethods_HasWellKnownMethodNames()
    {
        Assert.Equal("initialize", TraceConstants.McpMethods.Initialize);
        Assert.Equal("tools/call", TraceConstants.McpMethods.ToolsCall);
        Assert.Equal("tools/list", TraceConstants.McpMethods.ToolsList);
        Assert.Equal("resources/read", TraceConstants.McpMethods.ResourcesRead);
        Assert.Equal("resources/list", TraceConstants.McpMethods.ResourcesList);
        Assert.Equal("prompts/get", TraceConstants.McpMethods.PromptsGet);
        Assert.Equal("prompts/list", TraceConstants.McpMethods.PromptsList);
        Assert.Equal("ping", TraceConstants.McpMethods.Ping);
    }

    [Fact]
    public void GenAiOperations_HasExecuteToolOperation()
    {
        Assert.Equal("execute_tool", TraceConstants.GenAiOperations.ExecuteTool);
    }

    [Fact]
    public void ExtensionActivitySource_HasCorrectName()
    {
        Assert.Equal("Azure.Functions.Extensions.Mcp", TraceConstants.ExtensionActivitySource);
    }
}
