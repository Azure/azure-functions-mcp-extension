// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests;

public abstract class BaseInitializationTests 
{
    private readonly McpEndToEndFixtureBase _fixture;

    public BaseInitializationTests(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
    }

    protected async Task AssertServerInfoEquality(HttpTransportMode mode, Implementation assertedImplementation)
    {
        var client = await _fixture.CreateClientAsync(mode);
        Assert.Equal(assertedImplementation.Name, client.ServerInfo.Name);
        Assert.Equal(assertedImplementation.Version, client.ServerInfo.Version);
        Assert.Equal(assertedImplementation.Title, client.ServerInfo.Title);
    }

    protected async Task AssertServerIncludesNoInstructions(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        Assert.Null(client.ServerInstructions);
    }

    protected async Task AssertServerHasExpectedCapabilities(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        
        // Verify that the server capabilities are present
        Assert.NotNull(client.ServerCapabilities);
        
        // Server should support tools capability
        Assert.NotNull(client.ServerCapabilities.Tools);
    }

    protected async Task AssertServerMaintainsSession(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        
        // Verify that the client has a session
        Assert.NotNull(client.SessionId);
        Assert.NotEmpty(client.SessionId);
        
        // Verify that multiple requests work with the same session
        var tools1 = await client.ListToolsAsync();
        var tools2 = await client.ListToolsAsync();

        // Both calls should succeed, indicating session is maintained
        Assert.NotNull(tools1);
        Assert.NotNull(tools2);
    }
}
