// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.InProc;

public class InProcPingTests(InProcProjectFixture fixture, ITestOutputHelper testOutputHelper) : BasePingTests(fixture, testOutputHelper), IClassFixture<InProcProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServerRespondsToClientPing(HttpTransportMode mode)
    {
        await AssertServerRespondsToClientPing(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    public async Task InProcServerSendsPingsWithinSixMinutes(HttpTransportMode mode)
    {
        await AssertServerSendsPingsWithinSixMinutes(mode);
    }
}
