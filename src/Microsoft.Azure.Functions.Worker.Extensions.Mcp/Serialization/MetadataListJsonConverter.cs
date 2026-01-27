// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Serialization;

/// <summary>
/// Streams serialization of metadata key-value pairs using colon-separated paths.
/// </summary>
internal sealed class MetadataListJsonConverter : JsonConverter<List<KeyValuePair<string, object?>>>
{
    public override List<KeyValuePair<string, object?>>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, List<KeyValuePair<string, object?>> metadata, JsonSerializerOptions options)
    {
        var root = new Node();

        foreach (var kvp in metadata)
        {
            Add(root, kvp.Key.AsSpan(), kvp.Value);
        }

        writer.WriteStartObject();
        WriteChildren(writer, root, options);
        writer.WriteEndObject();
    }

    private static void Add(Node current, ReadOnlySpan<char> path, object? value)
    {
        var nextSeparator = path.IndexOf(':');
        if (nextSeparator == -1)
        {
            var segment = path.ToString();
            current.Children ??= new Dictionary<string, Node>(StringComparer.Ordinal);

            if (!current.Children.TryGetValue(segment, out var leaf))
            {
                leaf = new Node();
                current.Children[segment] = leaf;
            }

            leaf.Children = null;
            leaf.Value = value;
            return;
        }

        var segmentName = path[..nextSeparator].ToString();
        current.Children ??= new Dictionary<string, Node>(StringComparer.Ordinal);

        if (!current.Children.TryGetValue(segmentName, out var child))
        {
            child = new Node();
            current.Children[segmentName] = child;
        }

        child.Value = null;
        Add(child, path[(nextSeparator + 1)..], value);
    }

    private static void WriteChildren(Utf8JsonWriter writer, Node current, JsonSerializerOptions options)
    {
        if (current.Children is null)
        {
            return;
        }

        foreach (var kvp in current.Children)
        {
            writer.WritePropertyName(kvp.Key);
            WriteNode(writer, kvp.Value, options);
        }
    }

    private static void WriteNode(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {
        if (node.Children is null)
        {
            JsonSerializer.Serialize(writer, node.Value, options);
            return;
        }

        writer.WriteStartObject();
        WriteChildren(writer, node, options);
        writer.WriteEndObject();
    }

    private sealed class Node
    {
        public Dictionary<string, Node>? Children { get; set; }
        public object? Value { get; set; }
    }
}
