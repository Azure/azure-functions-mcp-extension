// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Shared context passed to each builder step.
/// Holds the parsed bindings, function metadata, and cross-step shared state.
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
    /// Input schema resolved by <see cref="Steps.AddInputSchemaExtension"/>,
    /// consumed by <see cref="Steps.PatchPropertyBindingsExtension"/>.
    /// </summary>
    public JsonNode? ResolvedInputSchema { get; set; }
}
