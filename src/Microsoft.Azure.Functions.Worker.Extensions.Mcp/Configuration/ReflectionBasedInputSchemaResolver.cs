// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves the input schema by reflecting over the function's method signature
/// and generating a schema from its parameters.
/// </summary>
internal class ReflectionBasedInputSchemaResolver(
    ILogger<ReflectionBasedInputSchemaResolver> logger) : IInputSchemaResolver
{
    public bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out JsonNode? inputSchema)
    {
        if (InputSchemaGenerator.TryGenerateFromFunction(functionMetadata, out inputSchema, logger) && inputSchema is not null)
        {
            return true;
        }

        inputSchema = null;
        return false;
    }
}
