// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Defines a strategy for resolving the input schema for an MCP tool trigger function.
/// </summary>
internal interface IInputSchemaResolver
{
    /// <summary>
    /// Attempts to resolve an input schema for the given tool options and function metadata.
    /// </summary>
    /// <param name="toolOptions">The tool options containing explicit schema or property configuration.</param>
    /// <param name="functionMetadata">The function metadata for reflection-based schema generation.</param>
    /// <param name="inputSchema">The resolved input schema, if successful.</param>
    /// <returns>True if the schema was resolved, false otherwise.</returns>
    bool TryResolve(ToolOptions toolOptions, IFunctionMetadata functionMetadata, out JsonNode? inputSchema);
}
