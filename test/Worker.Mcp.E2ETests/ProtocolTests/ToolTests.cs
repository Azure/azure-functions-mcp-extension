// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class ToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ReturnsAllTools(HttpTransportMode mode)
    {
        await AssertListToolsReturnsExpectedCount(mode, 6);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ReturnsNonEmpty(HttpTransportMode mode)
    {
        await AssertListToolsReturnsNonEmpty(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListTools_ContainsExpectedTools(HttpTransportMode mode)
    {
        // Default server (TestAppIsolated) has these tools: HappyFunction, SingleArgumentFunction, 
        // SingleArgumentWithDefaultFunction, GetSnippet, SaveSnippet, SearchSnippets
        await AssertToolsListContainsTool(mode, "HappyFunction");
        await AssertToolsListContainsTool(mode, "SingleArgumentFunction");
        await AssertToolsListContainsTool(mode, "SingleArgumentWithDefaultFunction");
        await AssertToolsListContainsTool(mode, "getsnippets");
        await AssertToolsListContainsTool(mode, "savesnippet");
        await AssertToolsListContainsTool(mode, "searchsnippets");
    }

    private async Task AssertListToolsReturnsExpectedCount(HttpTransportMode mode, int expectedCount)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.Equal(expectedCount, tools.Count);
    }

    private async Task AssertListToolsReturnsNonEmpty(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.NotNull(tools);
        Assert.True(tools.Count > 0, "Expected at least one tool to be returned");
    }

    private async Task AssertToolsListContainsTool(HttpTransportMode mode, string expectedToolName)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync();

        Assert.NotNull(tools);
        Assert.Contains(tools, tool => tool.Name == expectedToolName);
    }
}
