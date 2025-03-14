using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;

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

        builder.AddExtension<McpExtensionConfigProvider>();

        return builder;
    }
}

