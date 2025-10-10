// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public class PingTests(DefaultProjectFixture fixture) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultServerRespondsToClientPing(HttpTransportMode mode)
    {
        var client = await _fixture.CreateClientAsync(mode);
        await client.PingAsync();
        // completion of this method indicates the server responded to the ping
        Assert.True(true, "Server responded to ping without exception");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    public async Task DefaultServerSendsPingsWithinSixMinutes(HttpTransportMode mode)
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
                        (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == System.Text.Json.JsonValueKind.Object && !resultProp.EnumerateObject().Any())
                        // or look for an error referencing the ping - this is specific to the curent version of the MCP client and is not a stable detection
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
