// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection;

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
}