// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Shared context passed between MCP binding-builder extension methods.
/// Holds the parsed bindings, function metadata, logger, and shared state used across the transformation pipeline.
/// </summary>
internal sealed class McpBuilderContext
{
    public McpBuilderContext(IFunctionMetadata function, List<McpParsedBinding> bindings, ILogger logger)
    {
        Function = function;
        Bindings = bindings;
        Logger = logger;
    }

    public IFunctionMetadata Function { get; }

    public List<McpParsedBinding> Bindings { get; }

    public ILogger Logger { get; }

    /// <summary>
    /// Tool properties resolved by <see cref="AddToolPropertiesExtension"/>,
    /// consumed by <see cref="PatchPropertyBindingsExtension"/>.
    /// </summary>
    public List<ToolProperty>? ResolvedToolProperties { get; set; }
}
