// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves the input schema by generating it from configured tool properties
/// set via <c>WithProperty(...)</c>.
/// </summary>
internal class PropertyBasedInputSchemaResolver : IInputSchemaResolver
{
    public bool TryResolve(ToolOptions toolOptions, IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        inputSchema = null;

        if (toolOptions.Properties.Count == 0)
        {
            return false;
        }

        inputSchema = InputSchemaGenerator.GenerateFromToolProperties(toolOptions.Properties);
        return true;
    }
}
