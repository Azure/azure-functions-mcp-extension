// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.Default;

public class DefaultPingTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : BasePingTests(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_RespondsToClientPing(HttpTransportMode mode)
    {
        await AssertServerRespondsToClientPing(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    public async Task DefaultServer_SendsPingsWithinSixMinutes(HttpTransportMode mode)
    {
        await AssertServerSendsPingsWithinSixMinutes(mode);
    }
}
