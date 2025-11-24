// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Worker.Mcp.E2ETests.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Abstractions;

// Prevent multiple instances of Core Tools from interfering with one another.
// If any new collection definitions are added, they should have parallelization disabled.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures
{
    public abstract class McpEndToEndFixtureBase(EndToEndTestProject project) : CoreToolsProjectBase(project)
    {
        private List<McpClient> _clients = [];

        public async Task<McpClient> CreateClientAsync(HttpTransportMode transportMode = HttpTransportMode.AutoDetect,
            McpClientOptions? clientOptions = null,
            ILoggerFactory? loggerFactory = null,
            DelegatingHandler? delegatingHandler = null,
            Func<JsonRpcNotification, CancellationToken, ValueTask>? notificationHandler = null)
        {
            if (IsFaulted)
            {
                LogErrorDetails();

                throw new InvalidOperationException("The test fixture is in a faulted state. See test output for details.");
            }

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = GetEndpointForTransport(transportMode),
                TransportMode = transportMode,
                Name = $"TestClient-{transportMode}"
            };

            HttpClientTransport transport;

            if (delegatingHandler is not null)
            {
                HttpClient httpClient = new HttpClient(delegatingHandler);
                transport = new HttpClientTransport(transportOptions, httpClient, loggerFactory, true); // letting disposal be handled by the IMcpClient
            }
            else
            {
                transport = new HttpClientTransport(transportOptions, loggerFactory);
            }

            var client = await McpClient.CreateAsync(transport, clientOptions);

            _clients.Add(client);

            return client;
        }

        private const string _mcpEndpointRelativePath = "/runtime/webhooks/mcp";
        private const string _sseRelativePath = $"{_mcpEndpointRelativePath}/sse";
        private Uri GetEndpointForTransport(HttpTransportMode transportMode)
        {
            if (AppRootEndpoint is null)
            {
                throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");
            }

            return transportMode switch
            {
                HttpTransportMode.Sse => new Uri(AppRootEndpoint, _sseRelativePath),
                HttpTransportMode.StreamableHttp => new Uri(AppRootEndpoint, _mcpEndpointRelativePath),
                HttpTransportMode.AutoDetect => new Uri(AppRootEndpoint, _sseRelativePath),
                _ => throw new ArgumentOutOfRangeException(nameof(transportMode), transportMode, "Unsupported transport mode")
            };
        }

        public async override ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
        }

        public async override ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            foreach (var client in _clients)
            {
                await client.DisposeAsync();
            }
        }

    }

}
