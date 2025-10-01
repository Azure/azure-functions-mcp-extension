// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.InProc;

public class InProcToolTests(InProcProjectFixture fixture, ITestOutputHelper testOutputHelper) : BaseToolTests(fixture, testOutputHelper), IClassFixture<InProcProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcListTools_ReturnsAllTools(HttpTransportMode mode)
    {
        await AssertListToolsReturnsExpectedCount(mode, 3);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcListTools_ReturnsNonEmpty(HttpTransportMode mode)
    {
        await AssertListToolsReturnsNonEmpty(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcListTools_ContainsExpectedTools(HttpTransportMode mode)
    {
        // InProc server (TestApp) has these tools: GetSnippet, SaveSnippet, SearchSnippets
        await AssertToolsListContainsTool(mode, "getsnippets");
        await AssertToolsListContainsTool(mode, "savesnippet");
        await AssertToolsListContainsTool(mode, "searchSnippets");
    }
}
