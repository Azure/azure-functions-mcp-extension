// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;

namespace Microsoft.Azure.Functions.Worker.Builder;

public static class McpHostBuilderExtensions
{
    /// <summary>
    /// Configures an MCP tool with the specified name and returns a builder for fluent configuration.
    /// </summary>
    /// <param name="builder">The Functions Worker application builder.</param>
    /// <param name="toolName">The unique name of the tool to configure.</param>
    /// <returns>An <see cref="McpToolBuilder"/> instance for configuring the tool.</returns>
    public static McpToolBuilder ConfigureMcpTool(this IFunctionsWorkerApplicationBuilder builder, string toolName)
    {
        return new McpToolBuilder(builder, toolName);
    }

    /// <summary>
    /// Configures an MCP resource with the specified name and returns a builder for fluent configuration.
    /// </summary>
    /// <param name="builder">The Functions Worker application builder.</param>
    /// <param name="resourceName">The unique name of the resource to configure.</param>
    /// <returns>An <see cref="McpResourceBuilder"/> instance for configuring the resource.</returns>
    public static McpResourceBuilder ConfigureMcpResource(this IFunctionsWorkerApplicationBuilder builder, string resourceName)
    {
        return new McpResourceBuilder(builder, resourceName);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IFunctionsWorkerApplicationBuilder ConfigureMcpExtension(this IFunctionsWorkerApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IFunctionMetadataTransformer, McpFunctionMetadataTransformer>());

        builder.UseMiddleware<FunctionsMcpContextMiddleware>();

        builder.Services.Configure<WorkerOptions>(static (workerOption) =>
        {
            workerOption.InputConverters.RegisterAt<ToolInvocationContextConverter>(0);
        });

        return builder;
    }
}
