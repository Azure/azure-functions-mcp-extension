using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.WebJobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Script.Description;
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

        builder.Services.AddSingleton<IFunctionProvider, McpFunctionProvider>();
        builder.Services.AddSingleton<IMcpRequestHandler, DefaultMcpRequestHandler>();
        builder.Services.AddSingleton<IToolRegistry, DefaultToolRegistry>();
        builder.Services.AddSingleton<IMcpMessageHandlerManager, DefaultMcpMessageHandlerManager>();

        builder.AddExtension<McpExtensionConfigProvider>();

        return builder;
    }
}

