// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Constants and registry for synthetic MCP App function naming.
/// </summary>
internal static class McpAppUtilities
{
    private const string Prefix = "__McpApp_";
    private const string AssetsPrefix = "__McpAppAssets_";

    /// <summary>
    /// The length of the assets prefix, used for extracting tool names from asset function names.
    /// </summary>
    public static int AssetsPrefixLength => AssetsPrefix.Length;

    private static readonly ConcurrentDictionary<string, byte> _registry = new();

    /// <summary>
    /// Generates the synthetic function name for the view-serving function of the given tool.
    /// </summary>
    public static string SyntheticFunctionName(string toolName) => $"{Prefix}{toolName}";

    /// <summary>
    /// Generates the synthetic function name for the static-assets-serving function of the given tool.
    /// </summary>
    public static string SyntheticAssetsFunctionName(string toolName) => $"{AssetsPrefix}{toolName}";

    /// <summary>
    /// Returns true if the given function name is a registered synthetic MCP App function.
    /// </summary>
    public static bool IsSyntheticFunction(string name) => _registry.ContainsKey(name);

    /// <summary>
    /// Extracts the original tool name from a synthetic view function name.
    /// Caller must verify IsSyntheticFunction first.
    /// </summary>
    public static string ExtractToolName(string syntheticName) => syntheticName[Prefix.Length..];

    /// <summary>
    /// Registers a synthetic function name. Called at emit time during metadata transformation.
    /// Thread-safe.
    /// </summary>
    public static void Register(string syntheticName) => _registry.TryAdd(syntheticName, 0);

    /// <summary>
    /// Clears all registrations. Intended for testing only.
    /// </summary>
    internal static void ClearForTesting() => _registry.Clear();
}
