// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class InitializationTests(DefaultProjectFixture fixture) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_InfoReturnedSuccessfully(HttpTransportMode mode)
    {
        var assertedImplementation = new Implementation()
        {
            Name = "Test project server",
            Version = "2.1.0"
        };
        
        var client = await _fixture.CreateClientAsync(mode);
        Assert.Equal(assertedImplementation.Name, client.ServerInfo.Name);
        Assert.Equal(assertedImplementation.Version, client.ServerInfo.Version);
        Assert.Equal(assertedImplementation.Title, client.ServerInfo.Title);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultInstructions_ReturnedSuccessfully(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        Assert.Equal("These instructions are only meant for testing and can be ignored.", client.ServerInstructions);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_HasExpectedCapabilities(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        
        // Verify that the server capabilities are present
        Assert.NotNull(client.ServerCapabilities);
        
        // Server should support tools capability
        Assert.NotNull(client.ServerCapabilities.Tools);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_MaintainsSession(HttpTransportMode mode)
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
