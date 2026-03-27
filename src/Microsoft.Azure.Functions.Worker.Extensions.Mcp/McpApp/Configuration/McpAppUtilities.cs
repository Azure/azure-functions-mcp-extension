// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Constants and naming utilities for synthetic MCP App functions.
/// </summary>
internal static class McpAppUtilities
{
    private const string Prefix = "functions--mcpapp-";

    /// <summary>
    /// Generates the synthetic function name for the view-serving resource of the given tool.
    /// </summary>
    public static string SyntheticFunctionName(string toolName) => $"{Prefix}{toolName}";

    /// <summary>
    /// Generates the <c>ui://</c> resource URI for the given tool name, per the MCP Apps spec.
    /// </summary>
    public static string ResourceUri(string toolName) => $"ui://{toolName}/view";

    /// <summary>
    /// Returns true if the given function name matches the synthetic MCP App function naming pattern.
    /// </summary>
    public static bool IsSyntheticFunction(string name) => name.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>
    /// Extracts the original tool name from a synthetic view function name.
    /// Caller must verify IsSyntheticFunction first.
    /// </summary>
    public static string ExtractToolName(string syntheticName) => syntheticName[Prefix.Length..];
}
