// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class InitializationTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task DefaultServer_InfoReturnedSuccessfully(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        Assert.Equal("Test project server", client.ServerInfo.Name);
        Assert.Equal("2.1.0", client.ServerInfo.Version);
    }

    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task DefaultInstructions_ReturnedSuccessfully(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        Assert.Equal("These instructions are only meant for testing and can be ignored.", client.ServerInstructions);
    }

    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task DefaultServer_HasExpectedCapabilities(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);

        Assert.NotNull(client.ServerCapabilities);
        Assert.NotNull(client.ServerCapabilities.Tools);
        Assert.NotNull(client.ServerCapabilities.Resources);
    }

    [Fact]
    public async Task DefaultServer_MaintainsSession_OverStreamableHttp()
    {
        var client = await Fixture.CreateClientAsync(HttpTransportMode.StreamableHttp);

        Assert.NotNull(client.SessionId);
        Assert.NotEmpty(client.SessionId);

        var tools1 = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var tools2 = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools1);
        Assert.NotNull(tools2);
    }
}
