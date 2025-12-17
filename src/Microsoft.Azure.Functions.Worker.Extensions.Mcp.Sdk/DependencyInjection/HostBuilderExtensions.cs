// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods to work with a <see cref="IFunctionsWorkerApplicationBuilder"/>.
/// </summary>
public static class McpSdkHostBuilderExtensions
{
    /// <summary>
    /// Adds the services needed to integrate with the MCP SDK.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IFunctionsWorkerApplicationBuilder ConfigureMcpSdkExtension(this IFunctionsWorkerApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IFunctionMetadataTransformer, McpUseResultSchemaTransformer>());

        builder.UseMiddleware<FunctionsMcpToolResultMiddleware>();

        return builder;
    }
}

