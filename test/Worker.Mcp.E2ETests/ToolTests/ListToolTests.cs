// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

public class ListToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ReturnsExpectedCount(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(19, tools.Count);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ReturnsNonEmpty(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools);
        Assert.True(tools.Count > 0, "Expected at least one tool to be returned");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsSimpleTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "EchoTool");
        Assert.Contains(tools, t => t.Name == "EchoWithDefault");
        Assert.Contains(tools, t => t.Name == "VoidTool");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsTypedParameterTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "TypedParametersTool");
        Assert.Contains(tools, t => t.Name == "CollectionParametersTool");
        Assert.Contains(tools, t => t.Name == "GuidAndDateTimeTool");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsContentReturnTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "TextContentTool");
        Assert.Contains(tools, t => t.Name == "ImageContentTool");
        Assert.Contains(tools, t => t.Name == "ResourceLinkTool");
        Assert.Contains(tools, t => t.Name == "MultiContentTool");
        Assert.Contains(tools, t => t.Name == "StructuredContentTool");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsPocoTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "PocoInputTool");
        Assert.Contains(tools, t => t.Name == "PocoOutputTool");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsMetadataAndFluentTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "MetadataAttributeTool");
        Assert.Contains(tools, t => t.Name == "FluentMetadataTool");
        Assert.Contains(tools, t => t.Name == "FluentDefinedTool");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_ContainsMcpApps(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "HelloApp");
        Assert.Contains(tools, t => t.Name == "MinimalApp");
        Assert.Contains(tools, t => t.Name == "VisibilityApp");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_MetadataAttributeTool_ContainsMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "MetadataAttributeTool");
        Assert.NotNull(tool);
        Assert.NotNull(tool.ProtocolTool.Meta);
        Assert.True(tool.ProtocolTool.Meta.ContainsKey("version"), "Tool should contain 'version' metadata");
        Assert.Equal(1.0, ((JsonNode)tool.ProtocolTool.Meta["version"]!).GetValue<double>());
        Assert.True(tool.ProtocolTool.Meta.ContainsKey("author"), "Tool should contain 'author' metadata");
        Assert.Equal("Jane Doe", ((JsonNode)tool.ProtocolTool.Meta["author"]!).GetValue<string>());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListTools_FluentMetadataTool_ContainsBuilderMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "FluentMetadataTool");
        Assert.NotNull(tool);
        Assert.NotNull(tool.ProtocolTool.Meta);
        Assert.True(tool.ProtocolTool.Meta.ContainsKey("imageVersion"), "Tool should contain 'imageVersion' metadata");
        Assert.Equal("1.0", ((JsonNode)tool.ProtocolTool.Meta["imageVersion"]!).GetValue<string>());
        Assert.True(tool.ProtocolTool.Meta.ContainsKey("source"), "Tool should contain 'source' metadata");
        Assert.Equal("builder", ((JsonNode)tool.ProtocolTool.Meta["source"]!).GetValue<string>());
    }
}
