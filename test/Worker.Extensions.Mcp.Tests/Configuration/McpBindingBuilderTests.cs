using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using Worker.Extensions.Mcp.Tests.Helpers;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class McpBindingBuilderTests
{
    [Fact]
    public void HasBindings_NoMcpBindings_ReturnsFalse()
    {
        var builder = CreateBuilder("{\"type\":\"httpTrigger\"}");

        Assert.False(builder.HasBindings);
    }

    [Fact]
    public void HasBindings_WithToolTrigger_ReturnsTrue()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        Assert.True(builder.HasBindings);
    }

    [Fact]
    public void HasBindings_WithResourceTrigger_ReturnsTrue()
    {
        var builder = CreateBuilder("{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}");

        Assert.True(builder.HasBindings);
    }

    [Fact]
    public void HasBindings_WithPromptTrigger_ReturnsTrue()
    {
        var builder = CreateBuilder("{\"type\":\"mcpPromptTrigger\",\"promptName\":\"MyPrompt\"}");

        Assert.True(builder.HasBindings);
    }

    [Fact]
    public void HasBindings_WithToolProperty_ReturnsTrue()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        Assert.True(builder.HasBindings);
    }

    [Fact]
    public void HasBindings_WithPromptArgument_ReturnsTrue()
    {
        var builder = CreateBuilder("{\"type\":\"mcpPromptArgument\",\"argumentName\":\"topic\"}");

        Assert.True(builder.HasBindings);
    }

    [Fact]
    public void ParseBindings_SkipsNonJsonObjects()
    {
        var builder = CreateBuilder("\"just a string\"", "123", "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}");

        Assert.True(builder.HasBindings);
        Assert.Single(builder.Context.Bindings);
    }

    [Fact]
    public void ParseBindings_SkipsObjectsWithoutType()
    {
        var builder = CreateBuilder("{\"name\":\"noType\"}", "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}");

        Assert.Single(builder.Context.Bindings);
        Assert.Equal("mcpToolTrigger", builder.Context.Bindings[0].BindingType);
    }

    [Fact]
    public void ParseBindings_ExtractsCorrectIdentifiers()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}",
            "{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}",
            "{\"type\":\"mcpPromptTrigger\",\"promptName\":\"MyPrompt\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}",
            "{\"type\":\"mcpPromptArgument\",\"argumentName\":\"topic\"}");

        Assert.Equal(5, builder.Context.Bindings.Count);
        Assert.Equal("MyTool", builder.Context.Bindings[0].Identifier);
        Assert.Equal("file://test", builder.Context.Bindings[1].Identifier);
        Assert.Equal("MyPrompt", builder.Context.Bindings[2].Identifier);
        Assert.Equal("age", builder.Context.Bindings[3].Identifier);
        Assert.Equal("topic", builder.Context.Bindings[4].Identifier);
    }

    [Fact]
    public void ParseBindings_PreservesOriginalIndex()
    {
        var builder = CreateBuilder(
            "{\"type\":\"httpTrigger\"}",
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"queue\"}");

        Assert.Single(builder.Context.Bindings);
        Assert.Equal(1, builder.Context.Bindings[0].Index);
    }

    [Fact]
    public void Build_SerializesBindingsBackToRawBindings()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}");

        builder.Context.Bindings[0].JsonObject["custom"] = "value";
        builder.Build();

        var result = JsonNode.Parse(builder.Context.Function.RawBindings![0])!.AsObject();
        Assert.Equal("value", result["custom"]?.ToString());
    }

    [Fact]
    public void Build_OnlyWritesParsedBindingIndices()
    {
        var httpBinding = "{\"type\":\"httpTrigger\",\"direction\":\"in\"}";
        var fn = CreateFunctionMetadata(bindings: new List<string> { httpBinding, "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}" });
        var builder = new McpBindingBuilder(fn.Object, NullLogger.Instance);

        builder.Build();

        Assert.Equal(httpBinding, fn.Object.RawBindings![0]);
    }
}
