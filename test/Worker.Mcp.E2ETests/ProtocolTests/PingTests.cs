// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class PingTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task DefaultServerRespondsToClientPing(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        await client.PingAsync(cancellationToken: TestContext.Current.CancellationToken);
        // completion of this method indicates the server responded to the ping
        Assert.True(true, "Server responded to ping without exception");
    }

    [Fact]
    public async Task DefaultServerSendsPingsWithinSixMinutes()
    {
        var handler = new PingResponseHandler();
        var client = await Fixture.CreateClientAsync(HttpTransportMode.Sse, delegatingHandler: handler);

        var timeout = TimeSpan.FromMinutes(6);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!handler.PingReceived && sw.Elapsed < timeout)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.True(handler.PingReceived, "Ping was not received from the server within the expected time frame.");
    }

    private class PingResponseHandler() : DelegatingHandler(new HttpClientHandler())
    {
        public bool PingReceived { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null &&
                request.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var contentString = await request.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    using var doc = JsonDocument.Parse(contentString);
                    var root = doc.RootElement;

                    if (
                        // Look for a response with an empty object for the result
                        (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Object && !resultProp.EnumerateObject().Any())
                        // or look for an error referencing the ping (specific to the current MCP client, not a stable detection)
                        || (root.TryGetProperty("error", out var errorProp) && errorProp.TryGetProperty("message", out var messageProp) && messageProp.GetString() == "Method 'ping' is not available."))
                    {
                        PingReceived = true;
                    }
                }
                catch (JsonException)
                {
                    // Ignore invalid JSON
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
