// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

/// <summary>
/// Default server session tests
/// </summary>
public class SessionTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;
    protected readonly ITestOutputHelper TestOutputHelper = testOutputHelper;

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_ClientHasSession(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        
        Assert.NotNull(client.SessionId);
        Assert.NotEmpty(client.SessionId);
        
        TestOutputHelper.WriteLine($"Client session ID: {client.SessionId}");
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_SessionPersistsAcrossRequests(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        var originalSessionId = client.SessionId;
        
        Assert.NotNull(originalSessionId);
        Assert.NotEmpty(originalSessionId);
        
        // Make multiple requests and verify session ID doesn't change
        var tools1 = await client.ListToolsAsync();
        Assert.Equal(originalSessionId, client.SessionId);
        
        var tools2 = await client.ListToolsAsync();
        Assert.Equal(originalSessionId, client.SessionId);
        
        // Verify both requests succeeded
        Assert.NotNull(tools1);
        Assert.NotNull(tools2);
        
        TestOutputHelper.WriteLine($"Session persisted across multiple requests: {originalSessionId}");
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_UniqueSessionsForDifferentClients(HttpTransportMode mode)
    {
        var client1 = await _fixture.CreateClientAsync(mode);
        var client2 = await _fixture.CreateClientAsync(mode);
        
        Assert.NotNull(client1.SessionId);
        Assert.NotNull(client2.SessionId);
        Assert.NotEqual(client1.SessionId, client2.SessionId);
        
        TestOutputHelper.WriteLine($"Client 1 session: {client1.SessionId}");
        TestOutputHelper.WriteLine($"Client 2 session: {client2.SessionId}");
    }
}
