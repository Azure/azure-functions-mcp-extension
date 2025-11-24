// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

/// <summary>
/// Default server session tests
/// </summary>
public class SessionTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_ClientHasSession(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        
        Assert.NotNull(client.SessionId);
        Assert.NotEmpty(client.SessionId);
        
        TestOutputHelper.WriteLine($"Client session ID: {client.SessionId}");
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_UniqueSessionsForDifferentClients(HttpTransportMode mode)
    {
        var client1 = await Fixture.CreateClientAsync(mode);
        var client2 = await Fixture.CreateClientAsync(mode);
        
        Assert.NotNull(client1.SessionId);
        Assert.NotNull(client2.SessionId);
        Assert.NotEqual(client1.SessionId, client2.SessionId);
        
        TestOutputHelper.WriteLine($"Client 1 session: {client1.SessionId}");
        TestOutputHelper.WriteLine($"Client 2 session: {client2.SessionId}");
    }
}
