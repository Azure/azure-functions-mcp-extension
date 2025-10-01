// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.InProc;

public class InitializationTests_InProcServer(InProcProjectFixture fixture, ITestOutputHelper testOutputHelper) : BaseInitializationTests(fixture, testOutputHelper), IClassFixture<InProcProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcInfoReturnedSuccessfully(HttpTransportMode mode)
    {
        await AssertServerInfoEquality(mode, new Implementation()
        {
            Name = "Azure Functions MCP server",
            Version = "1.0.0"
        });
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServerIncludesNoInstructions(HttpTransportMode mode)
    {
        await AssertServerIncludesNoInstructions(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServerHasExpectedCapabilities(HttpTransportMode mode)
    {
        await AssertServerHasExpectedCapabilities(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServerMaintainsSession(HttpTransportMode mode)
    {
        await AssertServerMaintainsSession(mode);
    }
}
