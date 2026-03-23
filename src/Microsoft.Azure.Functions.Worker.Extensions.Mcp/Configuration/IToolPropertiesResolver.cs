// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Defines a strategy for resolving tool properties for an MCP tool trigger function.
/// </summary>
internal interface IToolPropertiesResolver
{
    /// <summary>
    /// Attempts to resolve tool properties for the given tool name and function metadata.
    /// </summary>
    /// <param name="toolName">The name of the tool, used to look up tool options.</param>
    /// <param name="functionMetadata">The function metadata for reflection-based resolution.</param>
    /// <param name="toolProperties">The resolved tool properties, if successful.</param>
    /// <returns>True if tool properties were resolved, false otherwise.</returns>
    bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out List<ToolProperty>? toolProperties);
}
