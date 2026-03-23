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
    /// Attempts to resolve an input schema for the given tool name and function metadata.
    /// </summary>
    /// <param name="toolName">The name of the tool, used to look up tool options.</param>
    /// <param name="functionMetadata">The function metadata for reflection-based schema generation.</param>
    /// <param name="inputSchema">The resolved input schema, if successful.</param>
    /// <returns>True if the schema was resolved, false otherwise.</returns>
    bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out JsonNode? inputSchema);
}
