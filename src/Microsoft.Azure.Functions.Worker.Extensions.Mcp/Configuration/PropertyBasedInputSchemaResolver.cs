// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves the input schema by generating it from configured tool properties
/// set via <c>WithProperty(...)</c>.
/// </summary>
internal class PropertyBasedInputSchemaResolver(IOptionsMonitor<ToolOptions> toolOptionsMonitor) : IInputSchemaResolver
{
    public bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        inputSchema = null;

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.Properties.Count == 0)
        {
            return false;
        }

        inputSchema = InputSchemaGenerator.GenerateFromToolProperties(toolOptions.Properties);
        return true;
    }
}
