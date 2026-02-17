// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring metadata of an MCP resource within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and resource options.</param>
/// <param name="resourceUri">The unique URI of the resource to configure.</param>
public sealed class McpResourceBuilder(IFunctionsWorkerApplicationBuilder builder, string resourceUri)
    : McpBuilderBase<ResourceOptions, McpResourceBuilder>(builder, resourceUri)
{
}
