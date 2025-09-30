// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;

internal class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.None)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while trying to read Dictionary<string, object>.");
            }
        }

        // Support a null root object
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token for Dictionary<string, object> root. Found {reader.TokenType}.");
        }

        return ReadObject(ref reader);
    }

    private Dictionary<string, object> ReadObject(ref Utf8JsonReader reader)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token inside object. Found {reader.TokenType}.");
            }

            string propertyName = reader.GetString()!;

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON after property name.");
            }

            var value = ReadValue(ref reader);
            dictionary[propertyName] = value!;
        }

        throw new JsonException("Unexpected end of JSON while reading object.");
    }

    private List<object?> ReadArray(ref Utf8JsonReader reader)
    {
        // Assumes current token is StartArray
        var list = new List<object?>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            list.Add(ReadValue(ref reader));
        }

        throw new JsonException("Unexpected end of JSON while reading array.");
    }

    private object? ReadValue(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => ReadString(ref reader),
        JsonTokenType.Number => ReadNumber(ref reader),
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.Null => null,
        JsonTokenType.StartObject => ReadObject(ref reader),
        JsonTokenType.StartArray => ReadArray(ref reader),
        _ => throw new JsonException($"Unsupported JSON token: {reader.TokenType}")
    };

    private object ReadString(ref Utf8JsonReader reader)
    {

        // Also handle some common types that might be represented as strings
        if (reader.TryGetDateTimeOffset(out DateTimeOffset dto))
        {
            return dto;
        }

        string stringValue = reader.GetString()!;

        if (Guid.TryParse(stringValue, out Guid guid))
        {
            return guid;
        }

        return stringValue;
    }

    private object ReadNumber(ref Utf8JsonReader reader)
    {
        // Order of precedence for number types:
        // From smaller to larger types, we try to read the number as Int32, Int64, Decimal, and finally Double.


        if (reader.TryGetInt32(out int i))
        {
            return i;
        }

        if (reader.TryGetInt64(out long l))
        {
            return l;
        }

        if (reader.TryGetDecimal(out decimal dec))
        {
            return dec;
        }

        if (reader.TryGetDouble(out double dbl))
        {
            return dbl;
        }

        throw new JsonException("Unable to parse number token as Int32, Int64, Decimal, or Double.");
    }


    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        WriteObject(writer, value, options, visited);
    }

    private void WriteObject(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options, HashSet<object> visited)
    {
        if (!visited.Add(value))
        {
            throw new JsonException("Cycle detected while serializing Dictionary<string, object>.");
        }

        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            WriteDynamicValue(writer, kvp.Value, options, visited);
        }

        writer.WriteEndObject();
        visited.Remove(value);
    }

    private void WriteArray(Utf8JsonWriter writer, IEnumerable list, JsonSerializerOptions options, HashSet<object> visited)
    {
        writer.WriteStartArray();
        foreach (var item in list)
        {
            WriteDynamicValue(writer, item, options, visited);
        }
        writer.WriteEndArray();
    }

    private void WriteDynamicValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, HashSet<object> visited)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case decimal m:
                writer.WriteNumberValue(m);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt);
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                break;
            case Guid g:
                writer.WriteStringValue(g);
                break;
            case Dictionary<string, object> dict:
                WriteObject(writer, dict, options, visited);
                break;
            case IEnumerable enumerable when value is not string:
                if (!visited.Add(value))
                {
                    throw new JsonException("Cycle detected while serializing collection.");
                }
                WriteArray(writer, enumerable, options, visited);
                visited.Remove(value);
                break;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
