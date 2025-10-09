// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class ToolTests(DefaultProjectFixture fixture) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ReturnsAllTools(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.Equal(6, tools.Count);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ReturnsNonEmpty(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.NotNull(tools);
        Assert.True(tools.Count > 0, "Expected at least one tool to be returned");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ContainsExpectedTools(HttpTransportMode mode)
    {
        // Default server (TestAppIsolated) has these tools: HappyFunction, SingleArgumentFunction, 
        // SingleArgumentWithDefaultFunction, GetSnippet, SaveSnippet, SearchSnippets
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.NotNull(tools);
        Assert.Contains(tools, tool => tool.Name == "HappyFunction");
        Assert.Contains(tools, tool => tool.Name == "SingleArgumentFunction");
        Assert.Contains(tools, tool => tool.Name == "SingleArgumentWithDefaultFunction");
        Assert.Contains(tools, tool => tool.Name == "getsnippets");
        Assert.Contains(tools, tool => tool.Name == "savesnippet");
        Assert.Contains(tools, tool => tool.Name == "searchsnippets");
    }
}
