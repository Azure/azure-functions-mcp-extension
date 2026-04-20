using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
        var builder = new McpBindingBuilder(fn.Object, NullLogger.Instance, CreateToolOptions(), CreateResourceOptions(), CreatePromptOptions());

        builder.Build();

        Assert.Equal(httpBinding, fn.Object.RawBindings![0]);
    }

    // ── Input schema resolution: tools ──

    [Fact]
    public void Build_WithExplicitToolSchema_SetsInputSchemaAndUseWorkerFlag()
    {
        var explicitSchema = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";
        var builder = CreateBuilder(
            CreateToolOptions("MyTool", inputSchema: explicitSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.NotNull(binding.InputSchema);
        Assert.NotNull(builder.Context.ResolvedInputSchema);
    }

    [Fact]
    public void Build_WithResolvedToolProperties_GeneratesSchemaFromProperties()
    {
        var builder = CreateBuilder(
            CreateToolOptions("MyTool"), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        builder.Context.ResolvedToolProperties = [new ToolProperty("x", "string", "desc", true)];

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.NotNull(binding.InputSchema);
        var schema = JsonNode.Parse(binding.InputSchema)!.AsObject();
        Assert.Equal("object", schema["type"]?.ToString());
        var props = schema["properties"]!.AsObject();
        Assert.True(props.ContainsKey("x"));
    }

    [Fact]
    public void Build_ExplicitToolSchemaTakesPrecedenceOverResolvedProperties()
    {
        var explicitSchema = """{"type":"object","properties":{"explicit":{"type":"number"}},"required":[]}""";
        var builder = CreateBuilder(
            CreateToolOptions("MyTool", inputSchema: explicitSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        builder.Context.ResolvedToolProperties = [new ToolProperty("fromProp", "string", "desc", true)];

        builder.Build();

        var schema = JsonNode.Parse(builder.Context.Bindings[0].InputSchema!)!;
        var props = schema["properties"]!.AsObject();
        Assert.True(props.ContainsKey("explicit"));
        Assert.False(props.ContainsKey("fromProp"));
    }

    [Fact]
    public void Build_NoToolOptionsOrAttributes_SetsUseWorkerFlagButNoInputSchema()
    {
        var builder = CreateBuilder(
            CreateToolOptions("MyTool"), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.Null(binding.InputSchema);
        Assert.Null(builder.Context.ResolvedInputSchema);
    }

    [Fact]
    public void Build_ToolTriggerWithBlankIdentifier_SkipsSchemaResolution()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"\"}");

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.False(binding.UseWorkerInputSchema);
        Assert.Null(binding.InputSchema);
    }

    [Fact]
    public void Build_WhenToolSchemaResolutionFails_LogsWarning()
    {
        var mockLogger = new Mock<ILogger>();
        var fn = CreateFunctionMetadata(
            bindings: ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        var builder = new McpBindingBuilder(fn.Object, mockLogger.Object, CreateToolOptions("MyTool"), CreateResourceOptions(), CreatePromptOptions());

        builder.Build();

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to generate input schema")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Input schema resolution: prompts ──

    [Fact]
    public void Build_WithExplicitPromptSchema_SetsInputSchemaAndUseWorkerFlag()
    {
        var explicitSchema = """{"type":"object","properties":{"topic":{"type":"string"}},"required":["topic"]}""";
        var builder = CreateBuilder(
            CreateToolOptions(), CreateResourceOptions(), CreatePromptOptions("MyPrompt", inputSchema: explicitSchema),
            "{\"type\":\"mcpPromptTrigger\",\"promptName\":\"MyPrompt\"}");

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.NotNull(binding.InputSchema);
    }

    [Fact]
    public void Build_WithResolvedPromptArguments_GeneratesSchemaFromArguments()
    {
        var builder = CreateBuilder(
            CreateToolOptions(), CreateResourceOptions(), CreatePromptOptions("MyPrompt"),
            "{\"type\":\"mcpPromptTrigger\",\"promptName\":\"MyPrompt\"}");
        builder.Context.ResolvedPromptArguments =
        [
            new PromptArgumentDefinition("topic", "The topic", isRequired: true),
            new PromptArgumentDefinition("style", "The style")
        ];

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.NotNull(binding.InputSchema);
        var schema = JsonNode.Parse(binding.InputSchema)!.AsObject();
        Assert.Equal("object", schema["type"]?.ToString());
        var props = schema["properties"]!.AsObject();
        Assert.True(props.ContainsKey("topic"));
        Assert.True(props.ContainsKey("style"));
        var required = schema["required"]!.AsArray();
        Assert.Contains(required, r => r!.ToString() == "topic");
    }

    // ── Input schema resolution: resources ──

    [Fact]
    public void Build_WithExplicitResourceSchema_SetsInputSchemaAndUseWorkerFlag()
    {
        var explicitSchema = """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}""";
        var builder = CreateBuilder(
            CreateToolOptions(), CreateResourceOptions("file://test", inputSchema: explicitSchema), CreatePromptOptions(),
            "{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}");

        builder.Build();

        var binding = builder.Context.Bindings[0];
        Assert.True(binding.UseWorkerInputSchema);
        Assert.NotNull(binding.InputSchema);
    }

    // ── Property binding patching ──

    [Fact]
    public void Build_MatchingProperty_SetsPropertyType()
    {
        var inputSchema = """{"type":"object","properties":{"age":{"type":"integer","description":"age desc"}},"required":["age"]}""";
        var builder = CreateBuilder(
            CreateToolOptions("Tool", inputSchema: inputSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}");

        builder.Build();

        var json = JsonNode.Parse(builder.Context.Function.RawBindings![1])!.AsObject();
        Assert.Equal("integer", json["propertyType"]?.ToString());
    }

    [Fact]
    public void Build_NonMatchingProperty_NotPatched()
    {
        var inputSchema = """{"type":"object","properties":{"age":{"type":"integer","description":"age desc"}},"required":["age"]}""";
        var builder = CreateBuilder(
            CreateToolOptions("Tool", inputSchema: inputSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"unrelated\"}");

        builder.Build();

        var json = JsonNode.Parse(builder.Context.Function.RawBindings![1])!.AsObject();
        Assert.False(json.ContainsKey("propertyType"));
    }

    [Fact]
    public void Build_NoResolvedSchema_PropertyNotPatched()
    {
        var builder = CreateBuilder(
            CreateToolOptions("Tool"), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.Build();

        var json = JsonNode.Parse(builder.Context.Function.RawBindings![1])!.AsObject();
        Assert.False(json.ContainsKey("propertyType"));
    }

    [Fact]
    public void Build_MultipleProperties_PatchesAllMatching()
    {
        var inputSchema = """{"type":"object","properties":{"name":{"type":"string","description":"name desc"},"age":{"type":"integer","description":"age desc"}},"required":["name"]}""";
        var builder = CreateBuilder(
            CreateToolOptions("Tool", inputSchema: inputSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"unknown\"}");

        builder.Build();

        var json1 = JsonNode.Parse(builder.Context.Function.RawBindings![1])!.AsObject();
        var json2 = JsonNode.Parse(builder.Context.Function.RawBindings![2])!.AsObject();
        var json3 = JsonNode.Parse(builder.Context.Function.RawBindings![3])!.AsObject();
        Assert.Equal("string", json1["propertyType"]?.ToString());
        Assert.Equal("integer", json2["propertyType"]?.ToString());
        Assert.False(json3.ContainsKey("propertyType"));
    }

    [Fact]
    public void Build_OnlyPatchesToolPropertyBindings()
    {
        var inputSchema = """{"type":"object","properties":{"name":{"type":"string","description":"name desc"},"Tool":{"type":"string","description":"should not match"}},"required":["name"]}""";
        var builder = CreateBuilder(
            CreateToolOptions("Tool", inputSchema: inputSchema), CreateResourceOptions(), CreatePromptOptions(),
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.Build();

        var triggerJson = JsonNode.Parse(builder.Context.Function.RawBindings![0])!.AsObject();
        var propJson = JsonNode.Parse(builder.Context.Function.RawBindings![1])!.AsObject();
        Assert.False(triggerJson.ContainsKey("propertyType"));
        Assert.Equal("string", propJson["propertyType"]?.ToString());
    }
}
