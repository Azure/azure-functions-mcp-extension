// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
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
        var builder = CreateBuilder("tool", out var services);

        builder.WithProperty("name", McpToolPropertyType.String, "desc", required: true);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        var property = Assert.Single(options.Properties);
        Assert.Equal("name", property.Name);
        Assert.Equal("string", property.Type);
        Assert.Equal("desc", property.Description);
        Assert.True(property.IsRequired);
    }

    [Fact]
    public void WithProperty_ArrayType_SetsArrayMetadata()
    {
        var builder = CreateBuilder("tool", out var services);

        builder.WithProperty("tags", McpToolPropertyType.String.AsArray(), "desc");

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        var property = Assert.Single(options.Properties);
        Assert.Equal("string", property.Type);
        Assert.True(property.IsArray);
    }

    [Fact]
    public void WithMetadata_AddsMetadataEntry()
    {
        var builder = CreateBuilder("tool", out var services);

        builder.WithMetadata("openai/strict", true);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/strict"]!);
    }

    [Fact]
    public void WithInputSchema_String_SetsInputSchema()
    {
        var builder = CreateBuilder("tool", out var services);
        const string schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"name\"]}";

        builder.WithInputSchema(schema);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        Assert.Equal(schema, options.InputSchema);
    }

    [Fact]
    public void WithInputSchema_JsonNode_SetsInputSchema()
    {
        var builder = CreateBuilder("tool", out var services);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"integer\"}}}")!;

        builder.WithInputSchema(schemaNode);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        Assert.NotNull(options.InputSchema);
        var parsed = JsonNode.Parse(options.InputSchema!)!.AsObject();
        Assert.Equal("object", parsed["type"]!.GetValue<string>());
    }

    [Fact]
    public void WithInputSchema_Type_SetsGeneratedSchema()
    {
        var builder = CreateBuilder("tool", out var services);

        builder.WithInputSchema<TestInput>();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        Assert.NotNull(options.InputSchema);
        var schema = JsonNode.Parse(options.InputSchema!)!.AsObject();
        Assert.Equal("object", schema["type"]!.GetValue<string>());
        Assert.NotNull(schema["properties"]);
    }

    [Fact]
    public void WithInputSchema_InvalidJson_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.ThrowsAny<JsonException>(() => builder.WithInputSchema("not valid json"));
    }

    [Fact]
    public void WithInputSchema_PrimitiveType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var exception = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(int)));
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void WithInputSchema_InterfaceType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var exception = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(IDisposable)));
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void WithProperty_AfterWithInputSchema_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithInputSchema("{\"type\":\"object\",\"properties\":{}}");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.WithProperty("name", McpToolPropertyType.String, "desc"));

        Assert.Contains("mutually exclusive", exception.Message);
    }

    [Fact]
    public void WithInputSchema_AfterWithProperty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithProperty("name", McpToolPropertyType.String, "desc");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.WithInputSchema("{\"type\":\"object\",\"properties\":{}}"));

        Assert.Contains("mutually exclusive", exception.Message);
    }

    [Fact]
    public void WithMetadata_AndWithProperty_CanBeChained()
    {
        var builder = CreateBuilder("tool", out var services);

        builder
            .WithProperty("name", McpToolPropertyType.String, "desc", required: true)
            .WithMetadata("openai/strict", true);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get("tool");

        Assert.Single(options.Properties);
        Assert.Single(options.Metadata);
    }

    private sealed class TestInput
    {
        [Description("The city name")]
        public string City { get; set; } = string.Empty;

        public int Days { get; set; }
    }
}