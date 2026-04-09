// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools that test metadata attachment via [McpMetadata] attribute
/// and via the fluent API builder (.WithMetadata()).
/// </summary>
public class MetadataToolFunctions(ILogger<MetadataToolFunctions> logger)
{
    /// <summary>
    /// Tests the [McpMetadata] attribute on a tool trigger for declarative metadata.
    /// </summary>
    [Function(nameof(MetadataAttributeTool))]
    public string MetadataAttributeTool(
        [McpToolTrigger(nameof(MetadataAttributeTool), "A tool with metadata defined via attribute.")]
        [McpMetadata("""{"version": 1.0, "author": "Jane Doe"}""")]
        ToolInvocationContext context)
    {
        logger.LogInformation("MetadataAttributeTool invoked");
        return "Metadata attribute tool response";
    }

    /// <summary>
    /// Tests the fluent API .WithMetadata() on a tool. Metadata is configured in Program.cs.
    /// The tool itself has no [McpMetadata] attribute — all metadata comes from the builder.
    /// </summary>
    [Function(nameof(FluentMetadataTool))]
    public string FluentMetadataTool(
        [McpToolTrigger(nameof(FluentMetadataTool), "A tool with metadata defined via the fluent builder API.")] ToolInvocationContext context)
    {
        logger.LogInformation("FluentMetadataTool invoked");
        return "Fluent metadata tool response";
    }
}
