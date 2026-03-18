// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpResourceBuilderTests
{
    private static McpResourceBuilder CreateBuilder(string resourceUri, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpResourceBuilder(appBuilder.Object, resourceUri);
    }

    [Fact]
    public void WithMeta_AddsMetadataEntry()
    {
        var resourceUri = "ui://my/resource.html";
        var builder = CreateBuilder(resourceUri, out var services);

        builder.WithMetadata("openai/widgetPrefersBorder", true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceUri);

        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/widgetPrefersBorder"]!);
    }

    [Fact]
    public void WithMeta_MultipleEntries_AddsAllMetadata()
    {
        var resourceUri = "ui://my/resource.html";
        var builder = CreateBuilder(resourceUri, out var services);

        builder
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .WithMetadata("key3", false);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceUri);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMeta_EmptyKey_Throws()
    {
        var builder = CreateBuilder("ui://my/resource.html", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMetadata(string.Empty, "value"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMeta_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("ui://my/resource.html", out _);
        var result = builder.WithMetadata("key", "value");

        Assert.Same(builder, result);
    }

    [Fact]
    public void Chaining_MultipleWithMeta_ConfiguresAllMetadata()
    {
        var resourceUri = "ui://my/resource.html";
        var builder = CreateBuilder(resourceUri, out var services);

        builder
            .WithMetadata("openai/widgetPrefersBorder", true)
            .WithMetadata("custom/setting", "enabled")
            .WithMetadata("version", 2);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceUri);

        Assert.Equal(3, options.Metadata.Count);
        Assert.True((bool)options.Metadata["openai/widgetPrefersBorder"]!);
        Assert.Equal("enabled", options.Metadata["custom/setting"]);
        Assert.Equal(2, options.Metadata["version"]);
    }

    [Fact]
    public void WithMeta_Batch_AddsAllMetadata()
    {
        var resourceUri = "ui://my/resource.html";
        var builder = CreateBuilder(resourceUri, out var services);

        builder.WithMetadata(
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", 42),
            new KeyValuePair<string, object?>("key3", false));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceUri);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMeta_Batch_EmptyKey_Throws()
    {
        var builder = CreateBuilder("ui://my/resource.html", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMetadata(
            new KeyValuePair<string, object?>("valid", "value"),
            new KeyValuePair<string, object?>(string.Empty, "value")));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMeta_Batch_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("ui://my/resource.html", out _);

        var result = builder.WithMetadata(
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", "value2"));

        Assert.Same(builder, result);
    }
}
