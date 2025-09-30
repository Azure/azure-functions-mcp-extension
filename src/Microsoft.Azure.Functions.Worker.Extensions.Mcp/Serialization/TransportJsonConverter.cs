// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Serialization;

public sealed class TransportJsonConverter : JsonConverter<Transport>
{
    public override Transport? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        if (!TryGetPropertyIgnoreCase(root, "name", out JsonElement namePropperty) || namePropperty.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Transport object must include a string 'name' property.");
        }

        var name = namePropperty.GetString() ?? string.Empty;

        TryGetPropertyIgnoreCase(root, "properties", out var properties);

        return name.ToLowerInvariant() switch
        {
            "http-streamable" or "http-sse" or "http" => CreateHttpTransport(name, properties),
            _ => throw new JsonException($"Unknown transport type '{name}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Transport value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);

        switch (value)
        {
            case HttpTransport http:
                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                writer.WritePropertyName("headers");
                writer.WriteStartObject();
                foreach (var kvp in http.Headers)
                {
                    writer.WriteString(kvp.Key, kvp.Value);
                }
                writer.WriteEndObject(); 
                writer.WriteEndObject(); 
                break;


            default:
                throw new NotSupportedException($"Transport type '{value.GetType().FullName}' is not supported for serialization.");
        }

        writer.WriteEndObject();
    }

    private static HttpTransport CreateHttpTransport(string name, JsonElement properties)
    {
        var transport = new HttpTransport(name);

        if (properties.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(properties, "headers", out var headers) &&
            headers.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in headers.EnumerateObject())
            {
                transport.Headers[header.Name] = header.Value.ValueKind switch
                {
                    JsonValueKind.String => header.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => header.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => header.Value.ToString() ?? string.Empty,
                };
            }
        }

        return transport;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        // Optimize for the common case first:
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
