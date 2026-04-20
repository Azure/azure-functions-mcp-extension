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
    IOptionsMonitor<PromptOptions> promptOptionsMonitor,
    ILogger<McpFunctionMetadataTransformer> logger)
    : IFunctionMetadataTransformer
{
    private readonly ILogger _logger = logger;

    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        var syntheticFunctions = new List<DefaultFunctionMetadata>();
        var emittedAppTools = new HashSet<string>();

        foreach (var function in original)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            var builder = new McpBindingBuilder(function, _logger, toolOptionsMonitor, resourceOptionsMonitor, promptOptionsMonitor, emittedAppTools);

            if (!builder.HasBindings)
            {
                continue;
            }

            builder
                .AddToolProperties()
                .AddPromptArguments()
                .AddMetadata()
                .AddAppUiMetadata()
                .PatchPropertyBindings()
                .Build();

            syntheticFunctions.AddRange(builder.Context.SyntheticFunctions);
        }

        // Add synthetic functions after iteration to avoid modifying collection during enumeration
        foreach (var synthetic in syntheticFunctions)
        {
            original.Add(synthetic);
        }
    }
}
