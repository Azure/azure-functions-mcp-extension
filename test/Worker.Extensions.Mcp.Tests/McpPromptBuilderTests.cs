// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpPromptBuilderTests
{
    private static McpPromptBuilder CreateBuilder(string promptName, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpPromptBuilder(appBuilder.Object, promptName);
    }

    [Fact]
    public void WithMetadata_AddsMetadataEntry()
    {
        var promptName = "code_review";
        var builder = CreateBuilder(promptName, out var services);

        builder.WithMetadata("category", "development");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<PromptOptions>>().Get(promptName);

        Assert.Single(options.Metadata);
        Assert.Equal("development", options.Metadata["category"]);
    }

    [Fact]
    public void WithMetadata_MultipleEntries_AddsAllMetadata()
    {
        var promptName = "code_review";
        var builder = CreateBuilder(promptName, out var services);

        builder
            .WithMetadata("category", "development")
            .WithMetadata("version", "1.0");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<PromptOptions>>().Get(promptName);

        Assert.Equal(2, options.Metadata.Count);
        Assert.Equal("development", options.Metadata["category"]);
        Assert.Equal("1.0", options.Metadata["version"]);
    }

    [Fact]
    public void WithMetadata_EmptyKey_Throws()
    {
        var builder = CreateBuilder("code_review", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMetadata(string.Empty, "value"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMetadata_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("code_review", out _);
        var result = builder.WithMetadata("key", "value");

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithMetadata_Batch_AddsAllMetadata()
    {
        var promptName = "code_review";
        var builder = CreateBuilder(promptName, out var services);

        builder.WithMetadata(
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", 42));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<PromptOptions>>().Get(promptName);

        Assert.Equal(2, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
    }

    [Fact]
    public void WithInputSchema_String_SetsInputSchemaOption()
    {
        var promptName = "summarize";
        var builder = CreateBuilder(promptName, out var services);
        var schema = """{"type":"object","properties":{"topic":{"type":"string"}},"required":["topic"]}""";

        builder.WithInputSchema(schema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<PromptOptions>>().Get(promptName);

        Assert.NotNull(options.InputSchema);
        var expected = System.Text.Json.Nodes.JsonNode.Parse(schema)!;
        var actual = System.Text.Json.Nodes.JsonNode.Parse(options.InputSchema!)!;
        Assert.Equal(expected.ToJsonString(), actual.ToJsonString());
    }

    [Fact]
    public void WithInputSchema_InvalidJson_Throws()
    {
        var builder = CreateBuilder("summarize", out _);

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => builder.WithInputSchema("not valid json{"));
    }

    [Fact]
    public void WithInputSchema_NonObjectType_Throws()
    {
        var builder = CreateBuilder("summarize", out _);

        Assert.Throws<ArgumentException>(() => builder.WithInputSchema("""{"type":"string"}"""));
    }

    [Fact]
    public void WithInputSchema_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("summarize", out _);
        var result = builder.WithInputSchema("""{"type":"object","properties":{},"required":[]}""");

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_Type_SetsInputSchemaFromClrType()
    {
        var promptName = "summarize";
        var builder = CreateBuilder(promptName, out var services);

        builder.WithInputSchema<TestPromptInput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<PromptOptions>>().Get(promptName);

        Assert.NotNull(options.InputSchema);
        var schema = System.Text.Json.Nodes.JsonNode.Parse(options.InputSchema!)!.AsObject();
        Assert.Equal("object", schema["type"]?.ToString());
        Assert.NotNull(schema["properties"]);
    }

    private class TestPromptInput
    {
        public string? Topic { get; set; }
    }
}
