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
        ArgumentNullException.ThrowIfNull(builder);

        // Uncomment the line below to register the endpoints as functions.
        //builder.Services.AddSingleton<IFunctionProvider, McpFunctionProvider>();

        // Request handlers
        builder.Services.AddSingleton<IMcpRequestHandler, DefaultRequestHandler>();
        builder.Services.AddSingleton<ISseRequestHandler, SseRequestHandler>();
        builder.Services.AddSingleton<IStreamableHttpRequestHandler, StreamableHttpRequestHandler>();

        // Tools
        builder.Services.AddSingleton<IToolRegistry, DefaultToolRegistry>();

        // Resources
        builder.Services.AddSingleton<IResourceRegistry, DefaultResourceRegistry>();

        // Core services
        builder.Services.AddSingleton<IMcpInstanceIdProvider, DefaultMcpInstanceIdProvider>();
        builder.Services.AddSingleton<IMcpClientSessionManager, McpClientSessionManager>();

        // Backplane
        builder.Services.AddSingleton<IMcpBackplaneService, BackplaneService>();
        builder.Services.AddSingleton<IMcpBackplane, AzureStorageBackplane>();
        builder.Services.AddSingleton<QueueServiceClientProvider>();
        builder.Services.AddAzureClientsCore();

        // Diagnostics (OTel MCP semantic conventions)
        builder.Services.AddSingleton<RequestActivityFactory>();
        builder.Services.AddSingleton<McpMetrics>();

        // MCP server
        builder.Services.ConfigureOptions<FunctionsMcpServerOptionsSetup>();
        builder.Services.AddMcpServer()
            .WithListToolsHandler(static (c, ct) =>
            {
                var toolRegistry = c.Services?.GetRequiredService<IToolRegistry>()
                    ?? throw new InvalidOperationException("Tool registry not properly registered.");

                return toolRegistry.ListToolsAsync(ct);
            })
            .WithCallToolHandler(static async (c, ct) =>
            {
                var toolRegistry = c.Services?.GetRequiredService<IToolRegistry>()
                    ?? throw new InvalidOperationException("Tool registry not properly registered.");

                if (c.Params is { Name: var name } && toolRegistry.TryGetTool(name, out var tool))
                {
                    return await tool.RunAsync(c, ct);
                }

                throw new McpProtocolException($"Unknown tool: '{c.Params?.Name}'", McpErrorCode.InvalidParams);
            })
            .WithListResourcesHandler(static (c, ct) =>
            {
                var resourceRegistry = c.Services?.GetRequiredService<IResourceRegistry>()
                    ?? throw new InvalidOperationException("Resource registry not properly registered.");

                return resourceRegistry.ListResourcesAsync(ct);
            })
            .WithReadResourceHandler(static async (c, ct) =>
            {
                var resourceRegistry = c.Services?.GetRequiredService<IResourceRegistry>()
                    ?? throw new InvalidOperationException("Resource registry not properly registered.");

                if (c.Params is { Uri: var uri} && resourceRegistry.TryGetResource(uri, out var resource))
                {
                    return await resource.ReadAsync(c, ct);
                }

                throw new McpProtocolException($"Unknown resource: '{c.Params?.Uri}'", McpErrorCode.InvalidParams);
            });

        // Extension configuration
        builder.AddExtension<McpExtensionConfigProvider>()
            .BindOptions<McpOptions>();

        return builder;
    }
}
