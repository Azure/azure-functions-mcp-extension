// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Default implementation of <see cref="IResourceRegistry"/>.
/// </summary>
internal sealed class DefaultResourceRegistry : IResourceRegistry
{
    private readonly Dictionary<string, IMcpResource> _staticResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IMcpResourceTemplate> _templates = new();
    private readonly object _lock = new();

    // Cached results - resources don't change after startup
    private ListResourcesResult? _cachedResourcesResult;
    private ListResourceTemplatesResult? _cachedTemplatesResult;

    /// <inheritdoc/>
    public void Register(IMcpResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        ResourceUriHelper.Validate(resource.Uri);

        lock (_lock)
        {
            if (resource is IMcpResourceTemplate template)
            {
                // Check for duplicate template URI
                if (_templates.Any(t => string.Equals(t.Uri, resource.Uri, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Resource with URI '{resource.Uri}' is already registered.");
                }

                _templates.Add(template);
            }
            else
            {
                if (!_staticResources.TryAdd(resource.Uri, resource))
                {
                    throw new InvalidOperationException($"Resource with URI '{resource.Uri}' is already registered.");
                }
            }

            // Invalidate caches
            _cachedResourcesResult = null;
            _cachedTemplatesResult = null;
        }
    }

    /// <inheritdoc/>
    public bool TryGetResource(string uri, [NotNullWhen(true)] out IMcpResource? resource)
    {
        ArgumentNullException.ThrowIfNull(uri);

        ResourceUriHelper.Validate(uri);

        if (_staticResources.TryGetValue(uri, out resource))
        {
            return true;
        }

        foreach (var template in _templates)
        {
            if (template.TemplateRegex.IsMatch(uri))
            {
                resource = template;
                return true;
            }
        }

        resource = null;
        return false;
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<IMcpResource> GetResources()
    {
        return [.. _staticResources.Values, .. _templates];
    }

    /// <inheritdoc/>
    public ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedResourcesResult is not null)
        {
            return new ValueTask<ListResourcesResult>(_cachedResourcesResult);
        }

        lock (_lock)
        {
            _cachedResourcesResult ??= new ListResourcesResult
            {
                Resources = [.. _staticResources.Values.Select(static resource => new Resource
                {
                    Uri = resource.Uri,
                    Name = resource.Name,
                    Title = resource.Title,
                    Description = resource.Description,
                    MimeType = resource.MimeType,
                    Size = resource.Size,
                    Meta = MetadataParser.SerializeMetadata(resource.Metadata)
                })]
            };
        }

        return new ValueTask<ListResourcesResult>(_cachedResourcesResult);
    }

    /// <inheritdoc/>
    public ValueTask<ListResourceTemplatesResult> ListResourceTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedTemplatesResult is not null)
        {
            return new ValueTask<ListResourceTemplatesResult>(_cachedTemplatesResult);
        }

        lock (_lock)
        {
            _cachedTemplatesResult ??= new ListResourceTemplatesResult
            {
                ResourceTemplates = [.. _templates.Select(static resource => new ResourceTemplate
                {
                    UriTemplate = resource.Uri,
                    Name = resource.Name,
                    Description = resource.Description,
                    MimeType = resource.MimeType,
                    Meta = MetadataParser.SerializeMetadata(resource.Metadata)
                })]
            };
        }

        return new ValueTask<ListResourceTemplatesResult>(_cachedTemplatesResult);
    }
}
