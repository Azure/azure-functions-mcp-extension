// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for MCP extension.
/// </summary>
public static class McpWebJobsBuilderExtensions
{
    /// <summary>
    /// Adds the MCP extension to the provided <see cref="IWebJobsBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
    public static IWebJobsBuilder AddMcp(this IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(nameof(builder));

        // Uncomment the line below to register the endpoints as functions.
        //builder.Services.AddSingleton<IFunctionProvider, McpFunctionProvider>();

        // Request handlers
        builder.Services.AddSingleton<IMcpRequestHandler, DefaultRequestHandler>();
        builder.Services.AddSingleton<ISseRequestHandler, SseRequestHandler>();
        builder.Services.AddSingleton<IStreamableHttpRequestHandler, StreamableHttpRequestHandler>();

        // Tools
        builder.Services.AddSingleton<IToolRegistry, DefaultToolRegistry>();

        // Core services
        builder.Services.AddSingleton<IMcpInstanceIdProvider, DefaultMcpInstanceIdProvider>();
        builder.Services.AddSingleton<IMcpClientSessionManager, McpClientSessionManager>();

        // Backplane
        builder.Services.AddSingleton<IMcpBackplaneService, BackplaneService>();
        builder.Services.AddSingleton<IMcpBackplane, AzureStorageBackplane>();
        builder.Services.AddSingleton<QueueServiceClientProvider>();
        builder.Services.AddSingleton<RequestActivityFactory>();
        builder.Services.AddAzureClientsCore();

        // MCP server
        builder.Services.ConfigureOptions<FunctionsMcpServerOptionsSetup>();
        builder.Services.AddMcpServer()
            .WithListToolsHandler(static (c, ct) =>
            {
                var toolRegistry = c.Services?.GetRequiredService<IToolRegistry>();

                return toolRegistry is null
                    ? throw new InvalidOperationException("Tool registry not properly registered.")
                    : toolRegistry.ListToolsAsync(ct);
            })
            .WithCallToolHandler(static async (c, ct) =>
            {
                var toolRegistry = c.Services!.GetRequiredService<IToolRegistry>();

                if (c.Params is not null
                    && toolRegistry.TryGetTool(c.Params.Name, out var tool))
                {
                    return await tool.RunAsync(c.Params, ct);
                }

                throw new McpException($"Unknown tool: '{c.Params?.Name}'", McpErrorCode.InvalidParams);
            });

        // Extension configuration
        builder.AddExtension<McpExtensionConfigProvider>()
            .BindOptions<McpOptions>();

        return builder;
    }
}
