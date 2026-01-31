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
    private static McpResourceBuilder CreateBuilder(string resourceName, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpResourceBuilder(appBuilder.Object, resourceName);
    }

    [Fact]
    public void WithMeta_AddsMetadataEntry()
    {
        var resourceName = "myResource";
        var builder = CreateBuilder(resourceName, out var services);

        builder.WithMeta("openai/widgetPrefersBorder", true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceName);

        Assert.Single(options.Metadata);
        Assert.True((bool)options.Metadata["openai/widgetPrefersBorder"]!);
    }

    [Fact]
    public void WithMeta_MultipleEntries_AddsAllMetadata()
    {
        var resourceName = "myResource";
        var builder = CreateBuilder(resourceName, out var services);

        builder
            .WithMeta("key1", "value1")
            .WithMeta("key2", 42)
            .WithMeta("key3", false);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceName);

        Assert.Equal(3, options.Metadata.Count);
        Assert.Equal("value1", options.Metadata["key1"]);
        Assert.Equal(42, options.Metadata["key2"]);
        Assert.False((bool)options.Metadata["key3"]!);
    }

    [Fact]
    public void WithMeta_EmptyKey_Throws()
    {
        var builder = CreateBuilder("resource", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithMeta(string.Empty, "value"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void WithMeta_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("resource", out _);
        var result = builder.WithMeta("key", "value");

        Assert.Same(builder, result);
    }

    [Fact]
    public void Chaining_MultipleWithMeta_ConfiguresAllMetadata()
    {
        var resourceName = "myResource";
        var builder = CreateBuilder(resourceName, out var services);

        builder
            .WithMeta("openai/widgetPrefersBorder", true)
            .WithMeta("custom/setting", "enabled")
            .WithMeta("version", 2);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ResourceOptions>>().Get(resourceName);

        Assert.Equal(3, options.Metadata.Count);
        Assert.True((bool)options.Metadata["openai/widgetPrefersBorder"]!);
        Assert.Equal("enabled", options.Metadata["custom/setting"]);
        Assert.Equal(2, options.Metadata["version"]);
    }
}
