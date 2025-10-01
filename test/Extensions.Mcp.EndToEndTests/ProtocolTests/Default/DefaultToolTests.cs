// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.Default;

public class DefaultToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : BaseToolTests(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{
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
}
