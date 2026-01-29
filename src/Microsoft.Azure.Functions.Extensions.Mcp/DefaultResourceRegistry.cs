// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Default implementation of <see cref="IResourceRegistry"/>.
/// </summary>
internal sealed class DefaultResourceRegistry : IResourceRegistry
{
    private readonly Dictionary<string, IMcpResource> _resources = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void Register(IMcpResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        ValidateResourceUri(resource.Uri);

        if (!_resources.TryAdd(resource.Uri, resource))
        {
            throw new InvalidOperationException($"Resource with URI '{resource.Uri}' is already registered.");
        }
    }

    /// <inheritdoc/>
    public bool TryGetResource(string uri, [NotNullWhen(true)] out IMcpResource? resource)
    {
        ArgumentNullException.ThrowIfNull(uri);

        ValidateResourceUri(uri);

        return _resources.TryGetValue(uri, out resource);
    }

    /// <inheritdoc/>
    public ICollection<IMcpResource> GetResources()
    {
        return _resources.Values;
    }

    /// <inheritdoc/>
    public ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        var result = new ListResourcesResult
        {
            Resources = [.. _resources.Values.Select(static resource => new Resource
            {
                Uri = resource.Uri,
                Name = resource.Name,
                Description = resource.Description,
                MimeType = resource.MimeType,
                Size = resource.Size,
                Meta = Utility.BuildNestedMetadataJson(resource.Metadata)
            })]
        };

        return new ValueTask<ListResourcesResult>(result);
    }

    /// <summary>
    /// Validates a resource URI according to MCP security requirements.
    /// Servers MUST validate all resource URIs per the MCP specification.
    /// </summary>
    /// <param name="uri">The URI to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the URI is invalid.</exception>
    private static void ValidateResourceUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI cannot be null or whitespace.", nameof(uri));
        }

        // Validate URI format - require absolute URIs with a scheme
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) || !parsedUri.IsAbsoluteUri)
        {
            throw new ArgumentException($"Invalid resource URI format: '{uri}'", nameof(uri));
        }

        // TODO: Do we also want to validate the scheme here (e.g., only allow ui, file, etc.)?
        // This might be too restrictive depending on use cases.
    }
}
