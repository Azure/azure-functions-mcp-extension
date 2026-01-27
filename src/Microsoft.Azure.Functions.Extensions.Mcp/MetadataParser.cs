// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Utility class for parsing metadata from JSON strings.
/// </summary>
internal static class MetadataParser
{
    /// <summary>
    /// Parses a JSON metadata string into a dictionary.
    /// Supports nested objects and arrays with proper type conversion.
    /// </summary>
    /// <param name="metadataString">The JSON metadata string to parse.</param>
    /// <param name="logger">Optional logger for error reporting.</param>
    /// <returns>A dictionary containing the parsed metadata, or an empty dictionary if parsing fails.</returns>
    public static IReadOnlyDictionary<string, object?> ParseMetadata(string? metadataString, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(metadataString))
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

        // Prefer parsing through JsonDocument so we can materialize JsonElement values
        // into CLR primitives and avoid leaking disposable JsonDocument state.
        try
        {
            var parseOptions = new JsonDocumentOptions
            {
                // Limit depth to reduce resource usage
                MaxDepth = 32
            };

            using var doc = JsonDocument.Parse(metadataString, parseOptions);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                return ParseObjectMetadata(root);
            }

            var message = $"Metadata root must be a JSON object; received {root.ValueKind}.";
            logger?.LogError(message);
            throw new JsonException(message);
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to parse metadata from JSON string");
            throw;
        }
    }

    /// <summary>
    /// Serializes a metadata dictionary to a JsonObject for MCP protocol transmission.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to serialize.</param>
    /// <returns>A JsonObject containing the serialized metadata, or null if metadata is empty or null.</returns>
    public static JsonObject? SerializeMetadata(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(metadata)?.AsObject();
    }

    private static Dictionary<string, object?> ParseObjectMetadata(JsonElement root)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            dictionary[property.Name] = ConvertJsonElement(property.Value);
        }

        return dictionary;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => TryGetNumber(element, out var number) ? number : element.GetRawText(),
            JsonValueKind.Object => ConvertObject(element),
            JsonValueKind.Array => ConvertArray(element),
            _ => element.GetRawText()
        };
    }

    private static object ConvertObject(JsonElement element)
    {
        var dictionary = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertJsonElement(property.Value);
        }

        return dictionary;
    }

    private static object ConvertArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(ConvertJsonElement(item));
        }

        return list;
    }

    private static bool TryGetNumber(JsonElement element, out object? result)
    {
        if (element.TryGetInt64(out var l))
        {
            result = l;
            return true;
        }

        if (element.TryGetDouble(out var d))
        {
            result = d;
            return true;
        }

        if (element.TryGetDecimal(out var dec))
        {
            result = dec;
            return true;
        }

        result = null;
        return false;
    }
}
