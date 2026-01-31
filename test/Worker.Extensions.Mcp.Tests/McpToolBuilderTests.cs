// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    public void WithMeta_AddsMetadataEntry()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithMeta("openai/strict", true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/strict"]!);
    }

    [Fact]
    public void WithMeta_MultipleEntries_AddsAllMetadata()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithMeta("key1", "value1")
            .WithMeta("key2", 42)
            .WithMeta("key3", false);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMeta_EmptyKey_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMeta(string.Empty, "value"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMeta_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var result = builder.WithMeta("key", "value");

        Assert.Same(builder, result);
    }

    [Fact]
    public void Chaining_WithPropertyAndWithMeta_ConfiguresAll()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithProperty("name", McpToolPropertyType.String, "The name", required: true)
            .WithMeta("openai/strict", true)
            .WithProperty("count", McpToolPropertyType.Integer, "The count");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(2, options.Properties.Count);
        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/strict"]!);
    }
}
