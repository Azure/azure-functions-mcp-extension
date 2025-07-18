﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

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

        builder.Services.AddSingleton<IRequestHandler, DefaultRequestHandler>();
        builder.Services.AddSingleton<IToolRegistry, DefaultToolRegistry>();
        builder.Services.AddSingleton<IMessageHandlerManager, DefaultMessageHandlerManager>();
        builder.Services.AddSingleton<IMcpInstanceIdProvider, DefaultMcpInstanceIdProvider>();
        builder.Services.AddSingleton<IMcpBackplane, AzureStorageBackplane>();
        builder.Services.AddSingleton<QueueServiceClientProvider>();
        builder.Services.AddSingleton<RequestActivityFactory>();
        builder.Services.AddAzureClientsCore();

        builder.AddExtension<McpExtensionConfigProvider>()
            .BindOptions<McpOptions>();

        return builder;
    }
}

