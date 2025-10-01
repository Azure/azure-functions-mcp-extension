// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.ProtocolTests
{
    public abstract class BasePingTests
    {
        private readonly McpEndToEndFixtureBase _fixture;

        protected BasePingTests(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            //_fixture.TestLogs.UseTestLogger(testOutputHelper);
        }

        protected async Task AssertServerRespondsToClientPing(HttpTransportMode mode)
        {
            var client = await _fixture.CreateClientAsync(mode);
            await client.PingAsync();
            // completion of this method indicates the server responded to the ping
        }

        protected async Task AssertServerSendsPingsWithinSixMinutes(HttpTransportMode mode)
        {
            var handler = new PingResponseHandler();
            var client = await _fixture.CreateClientAsync(mode, delegatingHandler: handler);

            var timeout = TimeSpan.FromMinutes(6);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!handler.PingReceived && sw.Elapsed < timeout)
            {
                await Task.Delay(100);
            }

            Assert.True(handler.PingReceived, "Ping was not received from the server within the expected time frame.");
        }

        protected class PingResponseHandler() : DelegatingHandler(new HttpClientHandler())
        {
            public bool PingReceived { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Content != null &&
                    request.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var contentString = await request.Content.ReadAsStringAsync(cancellationToken);
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(contentString);
                        var root = doc.RootElement;
                        
                        if (
                            // Look for a response with an empty object for the result
                            (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == System.Text.Json.JsonValueKind.Object && !resultProp.EnumerateObject().Any())
                            // or look for an error referencing the ping - this is specific to the curent version of the MCP client and is not a stable detection
                            || (root.TryGetProperty("error", out var errorProp) && errorProp.TryGetProperty("message", out var messageProp) && messageProp.GetString() == "Method 'ping' is not available."))
                        {
                            PingReceived = true;
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Ignore invalid JSON
                    }
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
