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
    public McpBuilderContext(IFunctionMetadata function, List<McpParsedBinding> bindings, ILogger logger, HashSet<string>? sharedEmittedAppTools = null)
    {
        Function = function;
        Bindings = bindings;
        Logger = logger;
        EmittedAppTools = sharedEmittedAppTools ?? [];
    }

    public IFunctionMetadata Function { get; }

    public List<McpParsedBinding> Bindings { get; }

    public ILogger Logger { get; }

    /// <summary>
    /// Tool properties resolved by the AddToolProperties step,
    /// consumed by the PatchPropertyBindings step.
    /// </summary>
    public List<ToolProperty>? ResolvedToolProperties { get; set; }

    /// <summary>
    /// Prompt arguments resolved by the AddPromptArguments step.
    /// </summary>
    public List<PromptArgumentDefinition>? ResolvedPromptArguments { get; set; }

    /// <summary>
    /// Synthetic function metadata entries created during the build pipeline (e.g., MCP App view resources).
    /// The caller should add these to the original function list after iteration.
    /// </summary>
    public List<DefaultFunctionMetadata> SyntheticFunctions { get; } = [];

    /// <summary>
    /// Tracks tool names for which synthetic app functions have already been emitted,
    /// preventing duplicate generation.
    /// </summary>
    public HashSet<string> EmittedAppTools { get; } = [];
}
