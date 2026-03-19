// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

/// <summary>
/// A view source backed by a file on disk.
/// </summary>
/// <param name="Path">The path to the HTML file.</param>
public sealed record FileViewSource(string Path) : McpViewSource
{
    public override Task<string> GetContentAsync(CancellationToken cancellationToken)
        => File.ReadAllTextAsync(Path, cancellationToken);
}
