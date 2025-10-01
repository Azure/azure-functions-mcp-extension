// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.Default;

/// <summary>
/// Default server session tests
/// </summary>
public class DefaultSessionTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) 
    : BaseSessionTests(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_ClientHasSession(HttpTransportMode mode)
    {
        await AssertClientHasSession(mode);
    }


    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_SessionPersistsAcrossRequests(HttpTransportMode mode)
    {
        await AssertSessionPersistsAcrossRequests(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServer_UniqueSessionsForDifferentClients(HttpTransportMode mode)
    {
        await AssertUniqueSessionsForDifferentClients(mode);
    }
}
