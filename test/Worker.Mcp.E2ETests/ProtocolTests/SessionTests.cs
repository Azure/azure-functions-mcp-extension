// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

/// <summary>
/// Session-handling tests for the StreamableHttp transport (the only transport
/// that surfaces a session id).
/// </summary>
public class SessionTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task DefaultServer_AssignsUniqueSessionPerClient()
    {
        var client1 = await Fixture.CreateClientAsync(HttpTransportMode.StreamableHttp);
        var client2 = await Fixture.CreateClientAsync(HttpTransportMode.StreamableHttp);

        Assert.NotNull(client1.SessionId);
        Assert.NotEmpty(client1.SessionId);
        Assert.NotNull(client2.SessionId);
        Assert.NotEqual(client1.SessionId, client2.SessionId);
    }
}
