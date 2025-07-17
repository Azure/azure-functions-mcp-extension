// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEnd.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEnd.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEnd.ProtocolTests
{
    public class ToolTests: IClassFixture<DefaultProjectFixture>
    {
        private readonly DefaultProjectFixture _fixture;
        public ToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            //_fixture.TestLogs.UseTestLogger(testOutputHelper);
        }


        [Theory]
        [InlineData(HttpTransportMode.Sse)]
        [InlineData(HttpTransportMode.AutoDetect)]
        [InlineData(HttpTransportMode.StreamableHttp)]
        public async Task ListTools_ReturnsAllTools(HttpTransportMode mode)
        {
            var client = await _fixture.CreateClientAsync(mode);
            var tools = await client.ListToolsAsync();

            Assert.Equal(3, tools.Count);
        }

    }
}
