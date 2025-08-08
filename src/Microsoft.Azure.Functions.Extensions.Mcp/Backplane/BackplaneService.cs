// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane
{
    internal sealed class BackplaneService(IMcpClientSessionManager sessionManager, IMcpBackplane backplane, IMcpInstanceIdProvider instanceIdProvider, ILogger<BackplaneService> logger) : IMcpBackplaneService
    {
        private Task? _backplaneProcessingTask;
        private readonly string _instanceId = instanceIdProvider.InstanceId;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _backplaneProcessingTask = InitializeBackplaneProcessing(backplane.Messages);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping backplane message processing.");

            return _backplaneProcessingTask ?? Task.CompletedTask;
        }

        private async Task InitializeBackplaneProcessing(ChannelReader<McpBackplaneMessage> messages)
        {
            try
            {
                logger.LogInformation("Initializing backplane message processing.");

                await foreach (var message in messages.ReadAllAsync())
                {
                    _ = HandleMessageAsync(message.ClientId, message.Message, CancellationToken.None);
                }
            }
            catch (OperationCanceledException) when (messages.Completion.IsCompleted)
            {
                logger.LogInformation("Backplane message processing completed.");
            }
        }

        private async Task HandleMessageAsync(string clientId, JsonRpcMessage message, CancellationToken cancellationToken)
        {
            var result = await sessionManager.TryGetSessionAsync(clientId);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Invalid client id. The session for client '{clientId}' does not exist in instance '{_instanceId}'.");
            }

            await result.Session.HandleMessageAsync(message, cancellationToken);
        }

        public Task SendMessageAsync(JsonRpcMessage message, string instanceId, string clientId, CancellationToken cancellationToken)
            => backplane.SendMessageAsync(message, instanceId, clientId, cancellationToken);
    }
}
