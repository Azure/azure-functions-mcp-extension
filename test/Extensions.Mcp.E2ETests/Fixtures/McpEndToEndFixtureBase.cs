// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.E2ETests.Abstractions;
using Extensions.Mcp.E2ETests.AbstractionOverCoreTools;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// Prevent multiple instances of Core Tools from interfering with one another.
// If any new collection definitions are added, they should have parallelization disabled.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Extensions.Mcp.E2ETests.Fixtures
{
    public abstract class McpEndToEndFixtureBase : CoreToolsProjectBase
    {
        private List<IMcpClient> _clients = [];

        protected McpEndToEndFixtureBase(EndToEndTestProject project) : base(project) { }

        public async Task<IMcpClient> CreateClientAsync(HttpTransportMode transportMode = HttpTransportMode.AutoDetect,
            McpClientOptions? clientOptions = null,
            ILoggerFactory? loggerFactory = null,
            DelegatingHandler? delegatingHandler = null,
            Func<JsonRpcNotification, CancellationToken, ValueTask>? notificationHandler = null)
        {
            var transportOptions = new SseClientTransportOptions()
            {
                Endpoint = GetEndpointForTransport(transportMode),
                TransportMode = transportMode,
                Name = $"TestClient-{transportMode}"
            };

            SseClientTransport transport;

            if (delegatingHandler is not null)
            {
                HttpClient httpClient = new HttpClient(delegatingHandler);
                transport = new SseClientTransport(transportOptions, httpClient, loggerFactory, true); // letting disposal be handled by the IMcpClient
            }
            else
            {
                transport = new SseClientTransport(transportOptions, loggerFactory);
            }

            var client = await McpClientFactory.CreateAsync(transport, clientOptions);

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

        public async new Task InitializeAsync()
        {
            await base.InitializeAsync();
        }

        public async new Task DisposeAsync()
        {
            await base.DisposeAsync();

            foreach (var client in _clients)
            {
                await client.DisposeAsync();
            }
        }

    }

}
