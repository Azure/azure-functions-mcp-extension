// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEnd.Fixtures;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEnd.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEnd.ProtocolTests;

public abstract class InitializationTests 
{
    private readonly McpEndToEndFixtureBase _fixture;

    public InitializationTests(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        //_fixture.TestLogs.UseTestLogger(testOutputHelper);
    }

    protected async Task AssertServerInfoEquality(HttpTransportMode mode, Implementation assertedImplementation)
    {
        var client = await _fixture.CreateClientAsync(mode);
        Assert.Equal(assertedImplementation.Name, client.ServerInfo.Name);
        Assert.Equal(assertedImplementation.Version, client.ServerInfo.Version);
        Assert.Equal(assertedImplementation.Title, client.ServerInfo.Title);
    }
}

public class InitializationTests_DefaultServer(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : InitializationTests(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServerInfoReturnedSuccessfully(HttpTransportMode mode) => await AssertServerInfoEquality(mode, new Implementation()
    {
        Name = "Azure Functions MCP server",
        Version = "1.0.0"
    });

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServerIncludesNoInstructions(HttpTransportMode mode) {
        var client = await fixture.CreateClientAsync(mode);
        Assert.Null(client.ServerInstructions);
    }
}

public class InitializationTests_CustomServer(CustomServerProjectFixture fixture, ITestOutputHelper testOutputHelper) : InitializationTests(fixture, testOutputHelper), IClassFixture<CustomServerProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task CustomServerInfoReturnedSuccessfully(HttpTransportMode mode) => await AssertServerInfoEquality(mode, new Implementation()
    {
        Name = "Test project server",
        Version = "2.1.0"
    });

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task CustomInstructionsReturnedSuccessfully(HttpTransportMode mode)
    {
        var client = await fixture.CreateClientAsync(mode);
        Assert.Equal("These instructions are only meant for testing and can be ignored.", client.ServerInstructions);
    }


    // expected capability list

    // session being dropped leads to error
}
