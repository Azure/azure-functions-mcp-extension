// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests.InProc;

/// <summary>
/// InProc server session tests
/// </summary>
public class InProcSessionTests(InProcProjectFixture fixture, ITestOutputHelper testOutputHelper) 
    : BaseSessionTests(fixture, testOutputHelper), IClassFixture<InProcProjectFixture>
{
    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServer_ClientHasSession(HttpTransportMode mode)
    {
        await AssertClientHasSession(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServer_SessionPersistsAcrossRequests(HttpTransportMode mode)
    {
        await AssertSessionPersistsAcrossRequests(mode);
    }

    [Theory]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task InProcServer_UniqueSessionsForDifferentClients(HttpTransportMode mode)
    {
        await AssertUniqueSessionsForDifferentClients(mode);
    }
}
