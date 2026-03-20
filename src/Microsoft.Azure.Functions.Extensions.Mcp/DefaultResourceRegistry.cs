// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Default implementation of <see cref="IResourceRegistry"/>.
/// </summary>
internal sealed class DefaultResourceRegistry : IResourceRegistry
{
    private readonly Dictionary<string, IMcpResource> _staticResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(IMcpResource Template, Regex Matcher)> _templates = [];

    /// <inheritdoc/>
    public void Register(IMcpResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        ResourceUriHelper.Validate(resource.Uri);

        if (ResourceUriHelper.IsTemplate(resource.Uri))
        {
            // Check for structurally equivalent template URIs (e.g., {id} and {name} in the same position)
            var normalizedUri = ResourceUriHelper.NormalizeTemplateStructure(resource.Uri);
            var existing = _templates.FirstOrDefault(t =>
                string.Equals(ResourceUriHelper.NormalizeTemplateStructure(t.Template.Uri), normalizedUri, StringComparison.OrdinalIgnoreCase));

            if (existing.Template is not null)
            {
                throw new InvalidOperationException(
                    $"A resource template with an equivalent URI pattern is already registered. Existing: '{existing.Template.Uri}', New: '{resource.Uri}'.");
            }

            var regex = ResourceUriHelper.BuildTemplateRegex(resource.Uri);
            _templates.Add((resource, regex));
        }
        else if (!_staticResources.TryAdd(resource.Uri, resource))
        {
            throw new InvalidOperationException($"Resource with URI '{resource.Uri}' is already registered.");
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

        foreach (var (template, matcher) in _templates)
        {
            if (matcher.IsMatch(uri))
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
        return [.. _staticResources.Values, .. _templates.Select(t => t.Template)];
    }

    /// <inheritdoc/>
    public ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        var result = new ListResourcesResult
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

        return new ValueTask<ListResourcesResult>(result);
    }

    /// <inheritdoc/>
    public ValueTask<ListResourceTemplatesResult> ListResourceTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var result = new ListResourceTemplatesResult
        {
            ResourceTemplates = [.. _templates.Select(static entry => new ResourceTemplate
            {
                UriTemplate = entry.Template.Uri,
                Name = entry.Template.Name,
                Title = entry.Template.Title,
                Description = entry.Template.Description,
                MimeType = entry.Template.MimeType,
                Meta = MetadataParser.SerializeMetadata(entry.Template.Metadata)
            })]
        };

        return new ValueTask<ListResourceTemplatesResult>(result);
    }
}
