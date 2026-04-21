// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Shared context passed between MCP binding-builder extension methods.
/// Holds the parsed bindings, function metadata, logger, and shared state used across the transformation pipeline.
/// </summary>
internal sealed class McpBuilderContext
{
    public McpBuilderContext(
        IFunctionMetadata function,
        List<McpParsedBinding> bindings,
        ILogger logger,
        IOptionsMonitor<ToolOptions> toolOptions,
        IOptionsMonitor<ResourceOptions> resourceOptions,
        IOptionsMonitor<PromptOptions> promptOptions,
        HashSet<string>? sharedEmittedAppTools = null)
    {
        Function = function;
        Bindings = bindings;
        Logger = logger;
        ToolOptions = toolOptions;
        ResourceOptions = resourceOptions;
        PromptOptions = promptOptions;
        EmittedAppTools = sharedEmittedAppTools ?? [];

        ToolTriggerBindings = bindings.Where(b => b.BindingType == McpToolTriggerBindingType && !string.IsNullOrWhiteSpace(b.Identifier)).ToList();
        PromptTriggerBindings = bindings.Where(b => b.BindingType == McpPromptTriggerBindingType && !string.IsNullOrWhiteSpace(b.Identifier)).ToList();
        ResourceTriggerBindings = bindings.Where(b => b.BindingType == McpResourceTriggerBindingType && !string.IsNullOrWhiteSpace(b.Identifier)).ToList();
        ToolPropertyBindings = bindings.Where(b => b.BindingType == McpToolPropertyBindingType && b.Identifier is not null).ToList();
        PromptArgumentBindings = bindings.Where(b => b.BindingType == McpPromptArgumentBindingType && b.Identifier is not null).ToList();
    }

    public IFunctionMetadata Function { get; }

    public List<McpParsedBinding> Bindings { get; }

    /// <summary>Bindings of type <c>mcpToolTrigger</c> with a non-empty identifier.</summary>
    public IReadOnlyList<McpParsedBinding> ToolTriggerBindings { get; }

    /// <summary>Bindings of type <c>mcpPromptTrigger</c> with a non-empty identifier.</summary>
    public IReadOnlyList<McpParsedBinding> PromptTriggerBindings { get; }

    /// <summary>Bindings of type <c>mcpResourceTrigger</c> with a non-empty identifier.</summary>
    public IReadOnlyList<McpParsedBinding> ResourceTriggerBindings { get; }

    /// <summary>Bindings of type <c>mcpToolProperty</c> with a non-null identifier.</summary>
    public IReadOnlyList<McpParsedBinding> ToolPropertyBindings { get; }

    /// <summary>Bindings of type <c>mcpPromptArgument</c> with a non-null identifier.</summary>
    public IReadOnlyList<McpParsedBinding> PromptArgumentBindings { get; }

    public ILogger Logger { get; }

    public IOptionsMonitor<ToolOptions> ToolOptions { get; }

    public IOptionsMonitor<ResourceOptions> ResourceOptions { get; }

    public IOptionsMonitor<PromptOptions> PromptOptions { get; }

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
    public HashSet<string> EmittedAppTools { get; }
}
