// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class McpToolExtensions
{
    internal static JsonElement GetPropertiesInputSchema(this IMcpTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        // If an explicit InputSchema is provided and it's not null, use it directly.
        if (tool.ToolInputSchema.InputSchema is not null)
        {
            return tool.ToolInputSchema.InputSchema.RootElement;
        }

        var props = (tool.ToolInputSchema.Properties ?? [])
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

                    WriteTypeAndEnum(writer, p.PropertyType, p.EnumValues);

                    writer.WriteEndObject();
                }
                else
                {
                    WriteTypeAndEnum(writer, p.PropertyType, p.EnumValues);
                }

                writer.WriteString("description", p.Description ?? string.Empty);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            var required = props
                .Where(p => p.IsRequired)
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

    private static void WriteTypeAndEnum(Utf8JsonWriter writer, string propertyType, IReadOnlyList<string> enumValues)
    {
        writer.WriteString("type", propertyType);
        WriteEnumArrayIfPresent(writer, enumValues);
    }

    private static void WriteEnumArrayIfPresent(Utf8JsonWriter writer, IReadOnlyList<string> enumValues)
    {
        if (enumValues is null || enumValues.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("enum");
        writer.WriteStartArray();
        foreach (var enumValue in enumValues)
        {
            writer.WriteStringValue(enumValue);
        }

        writer.WriteEndArray();
    }
}
