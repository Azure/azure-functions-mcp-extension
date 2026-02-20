// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Chains multiple <see cref="IInputSchemaResolver"/> implementations in priority order,
/// returning the first successful resolution.
/// </summary>
internal class CompositeInputSchemaResolver(params IInputSchemaResolver[] resolvers) : IInputSchemaResolver
{
    public bool TryResolve(ToolOptions toolOptions, IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.TryResolve(toolOptions, functionMetadata, out inputSchema))
            {
                return true;
            }
        }

        inputSchema = null;
        return false;
    }
}
