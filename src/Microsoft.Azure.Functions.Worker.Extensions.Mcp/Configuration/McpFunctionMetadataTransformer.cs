// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal sealed class McpFunctionMetadataTransformer(
    IOptionsMonitor<ToolOptions> toolOptionsMonitor,
    IOptionsMonitor<ResourceOptions> resourceOptionsMonitor,
    ILoggerFactory loggerFactory)
    : IFunctionMetadataTransformer
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<McpFunctionMetadataTransformer>();

    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        foreach (var function in original)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            var builder = new McpBindingBuilder(function, _logger);

            if (!builder.HasBindings)
            {
                continue;
            }

            builder
                .AddInputSchema(toolOptionsMonitor)
                .AddMetadata(toolOptionsMonitor, resourceOptionsMonitor)
                .PatchPropertyBindings()
                .Build();
        }
    }
}
