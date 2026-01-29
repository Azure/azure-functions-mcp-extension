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
    public async Task DefaultListTools_ReturnsAllTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(10, tools.Count);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ReturnsNonEmpty(HttpTransportMode mode)
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
    public async Task DefaultListTools_ContainsExpectedTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools);
        // Default server (TestAppIsolated) has these tools registered:
        Assert.Contains(tools, tool => tool.Name == "GetFunctionsLogo");
        Assert.Contains(tools, tool => tool.Name == "HappyFunction");
        Assert.Contains(tools, tool => tool.Name == "MultiContentTypeFunction");
        Assert.Contains(tools, tool => tool.Name == "RenderImage");
        Assert.Contains(tools, tool => tool.Name == "BirthdayTracker");
        Assert.Contains(tools, tool => tool.Name == "SingleArgumentFunction");
        Assert.Contains(tools, tool => tool.Name == "SingleArgumentWithDefaultFunction");
        Assert.Contains(tools, tool => tool.Name == "getsnippets");
        Assert.Contains(tools, tool => tool.Name == "savesnippet");
        Assert.Contains(tools, tool => tool.Name == "searchsnippets");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ContainsMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var logoTool = tools.FirstOrDefault(t => t.Name == "GetFunctionsLogo");
        Assert.NotNull(logoTool);
        Assert.NotNull(logoTool.ProtocolTool.Meta);
        Assert.True(logoTool.ProtocolTool.Meta.ContainsKey("version"), "Tool should contain 'version' metadata");
        Assert.Equal(1.0, ((JsonNode)logoTool.ProtocolTool.Meta["version"]!).GetValue<double>());
        Assert.True(logoTool.ProtocolTool.Meta.ContainsKey("author"), "Tool should contain 'author' metadata");
        Assert.Equal("Jane Doe", ((JsonNode)logoTool.ProtocolTool.Meta["author"]!).GetValue<string>());
    }
}
