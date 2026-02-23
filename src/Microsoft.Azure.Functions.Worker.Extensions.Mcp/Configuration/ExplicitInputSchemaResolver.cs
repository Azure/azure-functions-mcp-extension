// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves the input schema from an explicitly configured JSON schema string
/// set via <c>WithInputSchema(string)</c> or <c>WithInputSchema(Type)</c>.
/// </summary>
internal class ExplicitInputSchemaResolver(IOptionsMonitor<ToolOptions> toolOptionsMonitor) : IInputSchemaResolver
{
    public bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        inputSchema = null;

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (string.IsNullOrWhiteSpace(toolOptions.InputSchema))
        {
            return false;
        }

        inputSchema = JsonNode.Parse(toolOptions.InputSchema);
        return true;
    }
}
