// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;

namespace Microsoft.Azure.Functions.Worker.Builder;

public static class McpHostBuilderExtensions
{
    public static McpToolBuilder ConfigureMcpTool(this IFunctionsWorkerApplicationBuilder builder, string toolName)
    {
        return new McpToolBuilder(builder, toolName);
    }

    public static IFunctionsWorkerApplicationBuilder EnableMcpToolMetadata(this IFunctionsWorkerApplicationBuilder builder)
    {
        builder.Services.Decorate<IFunctionMetadataProvider, McpFunctionMetadataProvider>();

        return builder;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IFunctionsWorkerApplicationBuilder ConfigureMcpExtension(this IFunctionsWorkerApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseMiddleware<FunctionsMcpContextMiddleware>();

        builder.Services.Configure<WorkerOptions>(static (workerOption) =>
        {
            workerOption.InputConverters.RegisterAt<ToolInvocationContextConverter>(0);
        });

        return builder;
    }
}
