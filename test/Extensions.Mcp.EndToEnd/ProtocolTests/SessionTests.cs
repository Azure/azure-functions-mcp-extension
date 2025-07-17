// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEnd.Fixtures;
using ModelContextProtocol.Client;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEnd.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEnd.ProtocolTests
{
    public class SessionTests : IClassFixture<DefaultProjectFixture>
    {
        private readonly DefaultProjectFixture _fixture;
        public SessionTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            //_fixture.TestLogs.UseTestLogger(testOutputHelper);
        }

        [Theory]
        [InlineData(HttpTransportMode.StreamableHttp)]
        public async Task ClientHasSession(HttpTransportMode mode)
        {
            var client = await _fixture.CreateClientAsync(mode);
            Assert.NotNull(client.SessionId);
        }

        [Theory]
        [InlineData(HttpTransportMode.StreamableHttp)]
        public async Task ServerRejectsRequestWithoutSession(HttpTransportMode mode)
        {
            var handler = new SessionRemovingHandler();
            var client = await _fixture.CreateClientAsync(mode, delegatingHandler: handler);
            await Assert.ThrowsAnyAsync<Exception>(async () => { await client.ListToolsAsync(); });
        }

        private class SessionRemovingHandler() : DelegatingHandler(new HttpClientHandler())
        {
            public bool PingReceived { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Remove("Mcp-Session-Id");
                request.Headers.Remove("mcp-session-id");

                return await base.SendAsync(request, cancellationToken);
            }
        }
    }

}
