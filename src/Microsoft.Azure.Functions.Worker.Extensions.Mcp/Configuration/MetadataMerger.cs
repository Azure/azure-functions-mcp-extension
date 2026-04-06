// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Utility for merging metadata from fluent API and attribute sources.
/// </summary>
internal static class MetadataMerger
{
    /// <summary>
    /// Merges two JSON metadata strings. Properties from the attributed metadata take precedence
    /// over fluent API metadata when keys overlap. Attributed metadata wins because attributes are
    /// declared directly on the function and are more explicit, whereas fluent API metadata is
    /// configured separately and is intended for defaults or shared configuration.
    /// </summary>
    internal static string MergeMetadata(string? fluentJson, string? attributedJson, out List<string> overlappingKeys)
    {
        var fluentNode = string.IsNullOrWhiteSpace(fluentJson)
            ? []
            : JsonNode.Parse(fluentJson)?.AsObject() ?? throw new InvalidOperationException($"Failed to parse fluent API metadata as JSON object: {fluentJson}");
        var attributedNode = string.IsNullOrWhiteSpace(attributedJson)
            ? []
            : JsonNode.Parse(attributedJson)?.AsObject() ?? throw new InvalidOperationException($"Failed to parse attributed metadata as JSON object: {attributedJson}");

        overlappingKeys = [];

        foreach (var property in attributedNode)
        {
            if (fluentNode.ContainsKey(property.Key))
            {
                overlappingKeys.Add(property.Key);
            }

            fluentNode[property.Key] = property.Value?.DeepClone();
        }

        return fluentNode.ToJsonString();
    }
}
