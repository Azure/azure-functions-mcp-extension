// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

/// <summary>
/// Represents a registry for MCP resources.
/// </summary>
internal interface IResourceRegistry
{
    /// <summary>
    /// Registers an MCP resource.
    /// </summary>
    /// <param name="resource"></param>
    void Register(IMcpResource resource);

    /// <summary>
    /// Tries to get an MCP resource by its URI.
    /// </summary>
    /// <param name="uri">The URI of the resource.</param>
    /// <param name="resource">The retrieved resource, if found.</param>
    /// <returns>True if the resource was found; otherwise, false.</returns>
    bool TryGetResource(string uri, [NotNullWhen(true)] out IMcpResource? resource);

    /// <summary>
    /// Gets all registered MCP resources.
    /// </summary>
    IReadOnlyCollection<IMcpResource> GetResources();

    /// <summary>
    /// Lists all registered MCP resources.
    /// </summary>
    ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken = default);
}