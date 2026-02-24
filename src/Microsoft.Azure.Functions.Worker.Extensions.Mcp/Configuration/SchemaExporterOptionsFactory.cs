// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Provides shared <see cref="JsonSchemaExporterOptions"/> for generating JSON schemas
/// from CLR types using <see cref="JsonSchemaExporter"/>.
/// Ensures consistent schema generation across both the fluent API and auto-generation paths.
/// </summary>
internal static class SchemaExporterOptionsFactory
{
    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> that uses <see cref="JsonSerializerDefaults.Web"/>
    /// (which includes <see cref="JsonNamingPolicy.CamelCase"/>) to match the property names
    /// produced when serializing structured content via the MCP SDK.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Creates a <see cref="JsonSchemaExporterOptions"/> configured for MCP tool schema generation.
    /// Includes <see cref="DescriptionAttribute"/> values on properties as <c>"description"</c> fields
    /// and treats null-oblivious reference types as non-nullable.
    /// </summary>
    public static JsonSchemaExporterOptions Create() => new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = (context, node) =>
        {
            var descriptionAttribute = context.PropertyInfo?.AttributeProvider?
                .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
                .OfType<DescriptionAttribute>()
                .FirstOrDefault();

            if (descriptionAttribute is not null && node is JsonObject schemaObject)
            {
                schemaObject["description"] = descriptionAttribute.Description;
            }

            return node;
        }
    };
}
