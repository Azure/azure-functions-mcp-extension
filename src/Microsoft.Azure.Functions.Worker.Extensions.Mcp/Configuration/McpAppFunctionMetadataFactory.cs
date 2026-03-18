// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Factory for creating synthetic <see cref="DefaultFunctionMetadata"/> instances
/// for MCP App HTTP endpoints (view serving and static assets).
/// </summary>
internal static class McpAppFunctionMetadataFactory
{
    /// <summary>
    /// Creates function metadata for the view-serving HTTP trigger of an MCP App tool.
    /// </summary>
    /// <param name="toolName">The MCP tool name.</param>
    /// <returns>A <see cref="DefaultFunctionMetadata"/> with an HTTP GET trigger routed to <c>mcp/ui/{toolName}</c>.</returns>
    internal static DefaultFunctionMetadata CreateViewFunction(string toolName)
    {
        var functionName = McpAppUtilities.SyntheticFunctionName(toolName);

        return new DefaultFunctionMetadata()
        {
            Name = functionName,
            Language = "dotnet-isolated",
            RawBindings =
            [
                $"{{\"name\":\"req\",\"type\":\"httpTrigger\",\"direction\":\"In\",\"authLevel\":\"anonymous\",\"methods\":[\"get\"],\"route\":\"mcp/ui/{toolName}\"}}",
                "{\"name\":\"$return\",\"type\":\"http\",\"direction\":\"Out\"}",
            ],
            EntryPoint = McpAppFunctions.ServeViewEntryPoint,
            ScriptFile = McpAppFunctions.ScriptFile,
        };
    }

    /// <summary>
    /// Creates function metadata for the static-assets-serving HTTP trigger of an MCP App tool.
    /// </summary>
    /// <param name="toolName">The MCP tool name.</param>
    /// <returns>A <see cref="DefaultFunctionMetadata"/> with an HTTP GET trigger routed to <c>mcp/ui/{toolName}/assets/{{*path}}</c>.</returns>
    internal static DefaultFunctionMetadata CreateStaticAssetsFunction(string toolName)
    {
        var functionName = McpAppUtilities.SyntheticAssetsFunctionName(toolName);

        return new DefaultFunctionMetadata()
        {
            Name = functionName,
            Language = "dotnet-isolated",
            RawBindings =
            [
                $"{{\"name\":\"req\",\"type\":\"httpTrigger\",\"direction\":\"In\",\"authLevel\":\"anonymous\",\"methods\":[\"get\"],\"route\":\"mcp/ui/{toolName}/assets/{{*path}}\"}}",
                "{\"name\":\"$return\",\"type\":\"http\",\"direction\":\"Out\"}",
            ],
            EntryPoint = McpAppFunctions.ServeStaticAssetEntryPoint,
            ScriptFile = McpAppFunctions.ScriptFile,
        };
    }
}
