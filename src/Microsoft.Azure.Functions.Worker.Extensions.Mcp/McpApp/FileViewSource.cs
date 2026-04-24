// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

/// <summary>
/// A view source backed by a file on disk.
/// </summary>
/// <param name="Path">
/// The path to the HTML file. If relative, it is resolved against the
/// application's base directory (<see cref="AppContext.BaseDirectory"/>)
/// rather than the current working directory. This avoids path resolution
/// issues in hosting environments (e.g., Azure Functions placeholder /
/// specialization) where the process working directory does not match the
/// deployed app's directory.
/// </param>
public sealed record FileViewSource(string Path) : McpViewSource
{
    /// <summary>
    /// Gets the absolute path used to read the file. Relative paths are resolved
    /// against <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public string ResolvedPath => System.IO.Path.IsPathRooted(Path)
        ? Path
        : System.IO.Path.Combine(AppContext.BaseDirectory, Path);

    public override Task<string> GetContentAsync(CancellationToken cancellationToken)
        => File.ReadAllTextAsync(ResolvedPath, cancellationToken);
}
