// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Provides a fluent builder for configuring metadata of an MCP prompt within a Functions Worker application.
/// </summary>
/// <param name="builder">The application builder used to configure services and prompt options.</param>
/// <param name="promptName">The unique name of the prompt to configure.</param>
public sealed class McpPromptBuilder(IFunctionsWorkerApplicationBuilder builder, string promptName)
    : McpBuilderBase<PromptOptions, McpPromptBuilder>(builder, promptName)
{
}
