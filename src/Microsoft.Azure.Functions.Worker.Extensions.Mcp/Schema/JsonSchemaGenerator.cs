// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Schema;

/// <summary>
/// Utility for generating JSON schemas from tool properties.
/// Reuses the same logic as input schema generation for consistency.
/// </summary>
internal static class JsonSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema string from a list of tool properties.
    /// This creates a schema compatible with MCP tool input/output schema requirements.
    /// </summary>
    /// <param name="properties">The list of tool properties representing the type's properties.</param>
    /// <returns>A JSON string representing the schema, or null if no properties are provided.</returns>
    public static string? GenerateSchemaFromProperties(List<ToolProperty> properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return null;
        }

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            WriteSchemaObject(writer, properties);
        }

        ms.Position = 0;
        using var reader = new StreamReader(ms);
        return reader.ReadToEnd();
    }

    private static void WriteSchemaObject(Utf8JsonWriter writer, List<ToolProperty> properties)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "object");

        writer.WritePropertyName("properties");
        writer.WriteStartObject();

        foreach (var p in properties)
        {
            if (string.IsNullOrWhiteSpace(p.Name))
            {
                continue;
            }

            writer.WritePropertyName(p.Name);
            writer.WriteStartObject();

            if (p.IsArray)
            {
                writer.WriteString("type", "array");
                writer.WritePropertyName("items");
                writer.WriteStartObject();

                WriteTypeAndEnum(writer, p.Type, p.EnumValues);

                writer.WriteEndObject();
            }
            else
            {
                WriteTypeAndEnum(writer, p.Type, p.EnumValues);
            }

            writer.WriteString("description", p.Description ?? string.Empty);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();

        var required = properties
            .Where(p => p.IsRequired)
            .Select(p => p.Name)
            .Distinct()
            .ToArray();

        writer.WritePropertyName("required");
        writer.WriteStartArray();

        foreach (var r in required)
        {
            writer.WriteStringValue(r);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
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

