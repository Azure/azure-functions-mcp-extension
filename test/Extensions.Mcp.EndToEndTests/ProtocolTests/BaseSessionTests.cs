// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests;

/// <summary>
/// Abstract base class for session-related end-to-end tests
/// </summary>
public abstract class BaseSessionTests
{
    private readonly McpEndToEndFixtureBase _fixture;
    protected readonly ITestOutputHelper TestOutputHelper;

    protected BaseSessionTests(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        TestOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// Asserts that a client has a valid session when connecting
    /// </summary>
    protected async Task AssertClientHasSession(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        
        Assert.NotNull(client.SessionId);
        Assert.NotEmpty(client.SessionId);
        
        TestOutputHelper.WriteLine($"Client session ID: {client.SessionId}");
    }

    /// <summary>
    /// Asserts that sessions are properly maintained across multiple requests
    /// </summary>
    protected async Task AssertSessionPersistsAcrossRequests(HttpTransportMode mode)
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

    /// <summary>
    /// Asserts that each client gets a unique session
    /// </summary>
    protected async Task AssertUniqueSessionsForDifferentClients(HttpTransportMode mode)
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
