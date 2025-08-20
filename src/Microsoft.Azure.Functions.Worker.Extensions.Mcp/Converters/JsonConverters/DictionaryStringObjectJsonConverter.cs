// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;

internal class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);

        return ReadObject(doc.RootElement);
    }

    private Dictionary<string, object> ReadObject(JsonElement element)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ReadValue(property.Value)!;
        }

        return result;
    }

    private object? ReadValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element switch {
            _ when element.TryGetInt64(out var l) => l,
            _ when element.TryGetDouble(out var d) => d,
            _ => null
        },
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => ReadObject(element),
        JsonValueKind.Array => element.EnumerateArray().Select(ReadValue).ToList(),
        _ => throw new JsonException($"Unsupported JSON token: {element.ValueKind}")
    };

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        // Create options without the custom converter to prevent recursion
        var tempOptions = new JsonSerializerOptions(options);
        for (int i = tempOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (tempOptions.Converters[i] is DictionaryStringObjectJsonConverter)
            {
                tempOptions.Converters.RemoveAt(i);
            }
        }

        JsonSerializer.Serialize(writer, value, typeof(Dictionary<string, object>), tempOptions);
    }
}
