// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class McpToolExtensions
{
    private static readonly JsonSerializerOptions ToolSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static JsonElement GetPropertiesInputSchema(this IMcpTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        var props = (tool.Properties ?? [])
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.PropertyName))
            .ToArray();

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");

            writer.WritePropertyName("properties");
            writer.WriteStartObject();

            foreach (var p in props)
            {
                writer.WritePropertyName(p.PropertyName);
                writer.WriteStartObject();

                if (p.IsArray)
                {
                    writer.WriteString("type", "array");
                    writer.WritePropertyName("items");
                    writer.WriteStartObject();
                    writer.WriteString("type", p.PropertyType);
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteString("type", p.PropertyType);
                }

                writer.WriteString("description", p.Description ?? string.Empty);

                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            var required = props
                .Where(p => p.Required)
                .Select(p => p.PropertyName)
                .Distinct()
                .ToArray();


            // Always write the "required" property, even if there are no required properties.
            writer.WritePropertyName("required");
            writer.WriteStartArray();

            foreach (var r in required)
            {
                writer.WriteStringValue(r);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        ms.Position = 0;
        using var doc = JsonDocument.Parse(ms);
        return doc.RootElement.Clone();
    }
}
