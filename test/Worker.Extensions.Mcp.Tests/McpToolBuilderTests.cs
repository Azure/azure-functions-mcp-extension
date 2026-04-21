// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpToolBuilderTests
{
    private static McpToolBuilder CreateBuilder(string toolName, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpToolBuilder(appBuilder.Object, toolName);
    }

    [Fact]
    public void WithProperty_AddsToolProperty()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("prop1", McpToolPropertyType.String, "desc1", required: true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        var p = options.Properties[0];
        Assert.Equal("prop1", p.Name);
        Assert.Equal("string", p.Type);
        Assert.Equal("desc1", p.Description);
        Assert.True(p.IsRequired);
        Assert.False(p.IsArray);
    }

    [Fact]
    public void WithProperty_ArrayType_AddsArrayFlag()
    {
        var toolName = "arrayTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("numbers", McpToolPropertyType.Number.AsArray(), "numbers array");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        Assert.True(options.Properties[0].IsArray);
        Assert.Equal("number", options.Properties[0].Type);
    }

    [Fact]
    public void WithProperty_Chaining_AddsMultipleProperties()
    {
        var toolName = "chainTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithProperty("id", McpToolPropertyType.Integer, "identifier", required: true)
            .WithProperty("tags", McpToolPropertyType.String.AsArray(), "tags");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(2, options.Properties.Count);

        var id = options.Properties.First(p => p.Name == "id");
        Assert.Equal("integer", id.Type);
        Assert.True(id.IsRequired);
        Assert.False(id.IsArray);

        var tags = options.Properties.First(p => p.Name == "tags");
        Assert.Equal("string", tags.Type);
        Assert.False(tags.IsRequired);
        Assert.True(tags.IsArray);
    }

    [Fact]
    public void WithProperty_EmptyName_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty(string.Empty, McpToolPropertyType.Boolean, "desc"));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void WithProperty_NullType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var type = null as McpToolPropertyType;
        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.WithProperty("prop", type!, "desc"));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void Obsolete_WithProperty_StringOverload_AddsProperty()
    {
#pragma warning disable CS0618
        var toolName = "legacyTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("legacyProp", "string", "legacy description", required: true);
#pragma warning restore CS0618

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        var p = options.Properties[0];
        Assert.Equal("legacyProp", p.Name);
        Assert.Equal("string", p.Type);
        Assert.True(p.IsRequired);
        Assert.False(p.IsArray); // legacy overload cannot set arrays
    }

    [Fact]
    public void WithProperty_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var result = builder.WithProperty("p", McpToolPropertyType.Object, "desc");

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithMetadata_AddsMetadataEntry()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithMetadata("openai/strict", true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/strict"]!);
    }

    [Fact]
    public void WithMetadata_MultipleEntries_AddsAllMetadata()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .WithMetadata("key3", false);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMetadata_EmptyKey_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMetadata(string.Empty, "value"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMetadata_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var result = builder.WithMetadata("key", "value");

        Assert.Same(builder, result);
    }

    [Fact]
    public void Chaining_WithPropertyAndWithMetadata_ConfiguresAll()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithProperty("name", McpToolPropertyType.String, "The name", required: true)
            .WithMetadata("openai/strict", true)
            .WithProperty("count", McpToolPropertyType.Integer, "The count");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(2, options.Properties.Count);
        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/strict"]!);
    }

    [Fact]
    public void WithMetadata_Batch_AddsAllMetadata()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithMetadata(
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", 42),
            new KeyValuePair<string, object?>("key3", false));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMetadata_Batch_EmptyKey_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMetadata(
            new KeyValuePair<string, object?>("valid", "value"),
            new KeyValuePair<string, object?>(string.Empty, "value")));
        Assert.Equal("Key", ex.ParamName);
    }

    [Fact]
    public void WithMetadata_Batch_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);

        var result = builder.WithMetadata(
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", "value2"));

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_String_StoresSchemaInOptions()
    {
        var builder = CreateBuilder("tool", out var services);
        var schema = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";

        builder.WithInputSchema(schema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");
        Assert.Equal(schema, options.InputSchema);
    }

    [Fact]
    public void WithInputSchema_JsonNode_StoresSchemaInOptions()
    {
        var builder = CreateBuilder("tool", out var services);
        var node = JsonNode.Parse("""{"type":"object"}""")!;

        builder.WithInputSchema(node);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");
        Assert.NotNull(options.InputSchema);
        Assert.Contains("\"type\":\"object\"", options.InputSchema);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithInputSchema_String_NullOrEmpty_Throws(string? value)
    {
        var builder = CreateBuilder("tool", out _);
        Assert.ThrowsAny<ArgumentException>(() => builder.WithInputSchema(value!));
    }

    [Fact]
    public void WithInputSchema_String_InvalidJson_ThrowsJsonException()
    {
        var builder = CreateBuilder("tool", out _);
        Assert.ThrowsAny<JsonException>(() => builder.WithInputSchema("{not json"));
    }

    [Fact]
    public void WithInputSchema_String_NotObjectSchema_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        Assert.Throws<ArgumentException>(() => builder.WithInputSchema("""{"type":"string"}"""));
    }

    [Fact]
    public void WithInputSchema_JsonNode_Null_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        Assert.Throws<ArgumentNullException>(() => builder.WithInputSchema((JsonNode)null!));
    }

    [Fact]
    public void WithInputSchema_String_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder("tool", out _);
        var result = builder.WithInputSchema("""{"type":"object"}""");
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_CombinedWithWithProperty_BothPersist()
    {
        var builder = CreateBuilder("tool", out var services);

        builder.WithProperty("p", McpToolPropertyType.String, "desc")
               .WithInputSchema("""{"type":"object"}""");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");
        Assert.Single(options.Properties);
        Assert.NotNull(options.InputSchema);
    }
}
