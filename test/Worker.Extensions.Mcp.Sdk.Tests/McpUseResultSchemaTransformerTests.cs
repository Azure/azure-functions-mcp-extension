// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Moq;

namespace Worker.Extensions.Mcp.Sdk.Tests;

public class McpUseResultSchemaTransformerTests
{
    private readonly McpUseResultSchemaTransformer _transformer = new();

    [Fact]
    public void Transform_McpToolTrigger_NoOutputBindings_SetsUseResultSchemaTrue()
    {
        var function = CreateFunction(
            """{"type":"mcpToolTrigger","direction":"in","name":"trigger"}""");

        _transformer.Transform([function]);

        Assert.True(GetFlag(function.RawBindings![0]));
    }

    [Fact]
    public void Transform_McpPromptTrigger_NoOutputBindings_SetsUseResultSchemaTrue()
    {
        var function = CreateFunction(
            """{"type":"mcpPromptTrigger","direction":"in","name":"trigger"}""");

        _transformer.Transform([function]);

        Assert.True(GetFlag(function.RawBindings![0]));
    }

    [Fact]
    public void Transform_McpPromptTrigger_WithOutputBinding_DoesNotSetFlag()
    {
        var function = CreateFunction(
            """{"type":"mcpPromptTrigger","direction":"in","name":"trigger"}""",
            """{"type":"queue","direction":"out","name":"outQueue"}""");

        _transformer.Transform([function]);

        Assert.Null(GetFlag(function.RawBindings![0]));
    }

    [Fact]
    public void Transform_McpToolTrigger_WithOutputBinding_DoesNotSetFlag()
    {
        var function = CreateFunction(
            """{"type":"mcpToolTrigger","direction":"in","name":"trigger"}""",
            """{"type":"queue","direction":"out","name":"outQueue"}""");

        _transformer.Transform([function]);

        Assert.Null(GetFlag(function.RawBindings![0]));
    }

    [Fact]
    public void Transform_NonMcpBinding_IsIgnored()
    {
        var function = CreateFunction(
            """{"type":"httpTrigger","direction":"in","name":"req"}""");

        _transformer.Transform([function]);

        Assert.Null(GetFlag(function.RawBindings![0]));
    }

    [Fact]
    public void Transform_NullOrEmpty_DoesNotThrow()
    {
        _transformer.Transform([]);

        var nullBindings = new Mock<IFunctionMetadata>();
        nullBindings.SetupGet(x => x.RawBindings).Returns((IList<string>)null!);
        _transformer.Transform([nullBindings.Object]);
    }

    private static bool? GetFlag(string bindingJson)
    {
        var node = JsonNode.Parse(bindingJson) as JsonObject;
        return node?["useResultSchema"]?.GetValue<bool>();
    }

    private static IFunctionMetadata CreateFunction(params string[] rawBindings)
    {
        var mock = new Mock<IFunctionMetadata>();
        mock.SetupGet(x => x.RawBindings).Returns(rawBindings.ToList());
        return mock.Object;
    }
}
