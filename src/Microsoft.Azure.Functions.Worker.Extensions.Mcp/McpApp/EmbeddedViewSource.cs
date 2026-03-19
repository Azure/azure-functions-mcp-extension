// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

/// <summary>
/// A view source backed by a manifest resource embedded in an assembly.
/// </summary>
/// <param name="Assembly">The assembly containing the resource.</param>
/// <param name="ResourceName">The fully qualified manifest resource name.</param>
public sealed record EmbeddedViewSource(Assembly Assembly, string ResourceName) : McpViewSource
{
    public override Task<string> GetContentAsync(CancellationToken cancellationToken)
    {
        using var stream = Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found in assembly '{Assembly.FullName}'. " +
                $"Available resources: {string.Join(", ", Assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return Task.FromResult(reader.ReadToEnd());
    }
}
