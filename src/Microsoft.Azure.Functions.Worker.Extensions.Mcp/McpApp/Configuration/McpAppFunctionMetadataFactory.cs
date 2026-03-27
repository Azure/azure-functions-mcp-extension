// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Factory for creating synthetic <see cref="DefaultFunctionMetadata"/> instances
/// for MCP App resource endpoints that serve UI views via the <c>ui://</c> scheme.
/// </summary>
internal static class McpAppFunctionMetadataFactory
{
    internal const string AppMimeType = "text/html;profile=mcp-app";

    /// <summary>
    /// Creates function metadata for a synthetic MCP resource trigger that serves
    /// the view HTML for an MCP App tool via the <c>ui://</c> scheme.
    /// </summary>
    /// <param name="toolName">The MCP tool name.</param>
    /// <returns>A <see cref="DefaultFunctionMetadata"/> with an mcpResourceTrigger binding.</returns>
    internal static DefaultFunctionMetadata CreateViewResourceFunction(string toolName)
    {
        var functionName = McpAppUtilities.SyntheticFunctionName(toolName);
        var resourceUri = McpAppUtilities.ResourceUri(toolName);

        return new DefaultFunctionMetadata()
        {
            Name = functionName,
            Language = "dotnet-isolated",
            RawBindings =
            [
                $$"""{"name":"context","type":"mcpResourceTrigger","direction":"In","uri":"{{resourceUri}}","resourceName":"{{toolName}}_view","mimeType":"{{AppMimeType}}"}"""
            ],
            EntryPoint = McpAppFunctions.ServeViewEntryPoint,
            ScriptFile = McpAppFunctions.ScriptFile,
        };
    }
}
