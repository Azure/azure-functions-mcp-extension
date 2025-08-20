// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Worker.Builder;

public static class McpHostBuilderExtensions
{
    public static McpToolBuilder ConfigureMcpTool(this IFunctionsWorkerApplicationBuilder builder, string toolName)
    {
        return new McpToolBuilder(builder, toolName);
    }

    /// <summary>
    /// Enables MCP support with StreamableHttp transport by default (recommended since SSE is deprecated)
    /// </summary>
    /// <param name="builder">The functions worker application builder</param>
    /// <param name="configureOptions">Optional action to configure MCP options</param>
    /// <returns>The builder for method chaining</returns>
    public static IFunctionsWorkerApplicationBuilder EnableMcp(this IFunctionsWorkerApplicationBuilder builder, Action<McpOptions>? configureOptions = null)
    {
        // Configure options with StreamableHttp enabled by default
        var options = new McpOptions();
        configureOptions?.Invoke(options);

        // Register core MCP services
        RegisterCoreServices(builder, options);

        // Conditionally register StreamableHttp services based on configuration
        if (options.EnableStreamableHttp)
        {
            RegisterStreamableHttpServices(builder);
        }

        return builder;
    }

    public static IFunctionsWorkerApplicationBuilder EnableMcpToolMetadata(this IFunctionsWorkerApplicationBuilder builder)
    {
        // Register the metadata provider for tool property support
        RegisterFunctionMetadataProviderIfNotExists(builder);
        
        return builder;
    }

    /// <summary>
    /// Enables StreamableHttp support for MCP in Azure Functions isolated worker process
    /// </summary>
    /// <param name="builder">The functions worker application builder</param>
    /// <param name="configureOptions">Optional action to configure MCP options</param>
    /// <returns>The builder for method chaining</returns>
    public static IFunctionsWorkerApplicationBuilder EnableMcpStreamableHttp(this IFunctionsWorkerApplicationBuilder builder, Action<McpOptions>? configureOptions = null)
    {
        // Register core and StreamableHttp services
        RegisterCoreServices(builder, configureOptions);
        RegisterStreamableHttpServices(builder);

        return builder;
    }

    /// <summary>
    /// Disables StreamableHttp transport for MCP (not recommended as SSE is deprecated)
    /// </summary>
    /// <param name="builder">The functions worker application builder</param>
    /// <returns>The builder for method chaining</returns>
    public static IFunctionsWorkerApplicationBuilder DisableStreamableHttp(this IFunctionsWorkerApplicationBuilder builder)
    {
        builder.Services.Configure<McpOptions>(options => options.EnableStreamableHttp = false);
        return builder;
    }

    private static void RegisterCoreServices(IFunctionsWorkerApplicationBuilder builder, McpOptions? options = null)
    {
        // Register core MCP services with duplicate protection
        RegisterServiceIfNotExists<IMcpInstanceIdProvider, DefaultMcpInstanceIdProvider>(builder.Services);
        RegisterServiceIfNotExists<IMcpClientSessionManager, DefaultMcpClientSessionManager>(builder.Services);
        
        // Note: MCP Function Metadata Provider is not registered during auto-startup to avoid timing issues
        // It will be registered when EnableMcpToolMetadata() is explicitly called after the runtime is initialized
        
        // Register configuration options
        if (options != null)
        {
            builder.Services.Configure<McpOptions>(opts =>
            {
                opts.EncryptClientState = options.EncryptClientState;
                opts.EnableStreamableHttp = options.EnableStreamableHttp;
                opts.MessageOptions = options.MessageOptions;
            });
        }
        else
        {
            builder.Services.Configure<McpOptions>(options => { });
        }
        builder.Services.Configure<McpServerOptions>(options => { });
    }

    private static void RegisterCoreServices(IFunctionsWorkerApplicationBuilder builder, Action<McpOptions>? configureOptions)
    {
        var options = new McpOptions();
        configureOptions?.Invoke(options);
        RegisterCoreServices(builder, options);
    }

    private static void RegisterStreamableHttpServices(IFunctionsWorkerApplicationBuilder builder)
    {
        RegisterServiceIfNotExists<IStreamableHttpRequestHandler, StreamableHttpRequestHandler>(builder.Services);
        RegisterServiceIfNotExists<ISseRequestHandler, SseRequestHandler>(builder.Services);
        RegisterServiceIfNotExists<IMcpRequestHandler, DefaultRequestHandler>(builder.Services);
    }

    private static void RegisterServiceIfNotExists<TInterface, TImplementation>(IServiceCollection services)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        // Check if the service is already registered
        if (!services.Any(d => d.ServiceType == typeof(TInterface)))
        {
            services.AddSingleton<TInterface, TImplementation>();
        }
    }

    private static void RegisterFunctionMetadataProviderIfNotExists(IFunctionsWorkerApplicationBuilder builder)
    {
        // Check if MCP metadata provider is already registered to prevent duplicates
        var hasMetadataProvider = builder.Services.Any(d => d.ImplementationType == typeof(McpFunctionMetadataProvider));
        
        if (!hasMetadataProvider)
        {
            // Check if IFunctionMetadataProvider is available in the service collection
            var hasBaseMetadataProvider = builder.Services.Any(d => d.ServiceType == typeof(Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadataProvider));
            
            if (hasBaseMetadataProvider)
            {
                // Use custom decoration to enhance the existing IFunctionMetadataProvider with MCP functionality
                builder.Services.Decorate<Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadataProvider, McpFunctionMetadataProvider>();
            }
            // If base provider isn't available yet, skip decoration - this is expected during startup
            // The metadata provider will be registered later when the Functions runtime is fully initialized
        }
    }

}