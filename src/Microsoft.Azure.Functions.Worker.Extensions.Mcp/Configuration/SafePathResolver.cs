// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves requested paths against a root directory with traversal protection.
/// </summary>
internal static class SafePathResolver
{
    /// <summary>
    /// Resolves a requested path against a root directory, returning the full path
    /// if it is safe, or null if the request is a traversal attempt or the file
    /// does not exist.
    /// </summary>
    /// <param name="requestedPath">The raw path from the HTTP request (may be URL-encoded).</param>
    /// <param name="rootDirectory">The configured static assets root directory.</param>
    /// <param name="staticAssetOptions">Options controlling which files are served.</param>
    /// <returns>The resolved full path, or null if the request should be rejected.</returns>
    public static string? Resolve(
        string requestedPath,
        string rootDirectory,
        StaticAssetOptions? staticAssetOptions = null)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            return null;
        }

        // URL-decode to handle %2e%2e and similar encoding attacks
        var decoded = Uri.UnescapeDataString(requestedPath);

        // Reject null bytes (used in some traversal attacks)
        if (decoded.Contains('\0'))
        {
            return null;
        }

        // Normalize path separators to the OS convention
        decoded = decoded.Replace('/', Path.DirectorySeparatorChar)
                         .Replace('\\', Path.DirectorySeparatorChar);

        // Resolve to absolute path
        var rootPath = Path.GetFullPath(rootDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, decoded));

        // Ensure resolved path is within the root
        if (!fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !fullPath.Equals(rootPath, StringComparison.Ordinal))
        {
            return null;
        }

        // Check source map exclusion
        if (!(staticAssetOptions?.IncludeSourceMaps ?? false)
            && fullPath.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Verify file exists
        return File.Exists(fullPath) ? fullPath : null;
    }
}
