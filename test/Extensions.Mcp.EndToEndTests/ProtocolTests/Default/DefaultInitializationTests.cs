// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.Default;

public class InitializationTests_CustomServer(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : BaseInitializationTests(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_InfoReturnedSuccessfully(HttpTransportMode mode) => await AssertServerInfoEquality(mode, new Implementation()
    {
        Name = "Test project server",
        Version = "2.1.0"
    });

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultInstructions_ReturnedSuccessfully(HttpTransportMode mode)
    {
        var client = await fixture.CreateClientAsync(mode);
        Assert.Equal("These instructions are only meant for testing and can be ignored.", client.ServerInstructions);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_HasExpectedCapabilities(HttpTransportMode mode)
    {
        await AssertServerHasExpectedCapabilities(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_MaintainsSession(HttpTransportMode mode)
    {
        await AssertServerMaintainsSession(mode);
    }
}
