// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests
{
    public abstract class BaseToolTests
    {
        private readonly McpEndToEndFixtureBase _fixture;

        protected BaseToolTests(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            //_fixture.TestLogs.UseTestLogger(testOutputHelper);
        }

        protected async Task AssertListToolsReturnsExpectedCount(HttpTransportMode mode, int expectedCount)
        {
            var client = await _fixture.CreateClientAsync(mode);
            var tools = await client.ListToolsAsync();

            Assert.Equal(expectedCount, tools.Count);
        }

        protected async Task AssertListToolsReturnsNonEmpty(HttpTransportMode mode)
        {
            var client = await _fixture.CreateClientAsync(mode);
            var tools = await client.ListToolsAsync();

            Assert.NotNull(tools);
            Assert.True(tools.Count > 0, "Expected at least one tool to be returned");
        }

        protected async Task AssertToolsListContainsTool(HttpTransportMode mode, string expectedToolName)
        {
            var client = await _fixture.CreateClientAsync(mode);
            var tools = await client.ListToolsAsync();

            Assert.NotNull(tools);
            Assert.Contains(tools, tool => tool.Name == expectedToolName);
        }
    }
}
