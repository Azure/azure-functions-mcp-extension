// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Trigger
{
    /// <summary>
    /// Represents an MCP tool input schema that follows JSON Schema format.
    /// This class can be serialized to/from JSON to represent the expected input structure for MCP tools.
    /// </summary>
    public class McpInputSchema
    {
        /// <summary>
        /// Gets or sets the schema type. For MCP tools, this is typically "object".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        /// <summary>
        /// Gets or sets the properties definition for the schema.
        /// Each property defines the expected input parameters for the MCP tool.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, McpPropertySchema> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of required property names.
        /// Properties listed here must be provided when invoking the MCP tool.
        /// </summary>
        [JsonPropertyName("required")]
        public string[] Required { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets additional properties allowed indicator.
        /// When false, only properties defined in the Properties collection are allowed.
        /// </summary>
        [JsonPropertyName("additionalProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AdditionalProperties { get; set; }

        /// <summary>
        /// Gets or sets the schema title.
        /// </summary>
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the schema description.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Represents a property definition within an MCP input schema.
    /// </summary>
    public class McpPropertySchema
    {
        /// <summary>
        /// Gets or sets the property type (e.g., "string", "number", "boolean", "array", "object").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property description.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the items schema for array properties.
        /// This is only used when Type is "array".
        /// </summary>
        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public McpPropertySchema? Items { get; set; }

        /// <summary>
        /// Gets or sets the enum values for the property.
        /// Used to restrict the property to a specific set of values.
        /// </summary>
        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Enum { get; set; }

        /// <summary>
        /// Gets or sets the default value for the property.
        /// </summary>
        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Default { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for numeric properties.
        /// </summary>
        [JsonPropertyName("minimum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for numeric properties.
        /// </summary>
        [JsonPropertyName("maximum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Maximum { get; set; }

        /// <summary>
        /// Gets or sets the minimum length for string properties.
        /// </summary>
        [JsonPropertyName("minLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length for string properties.
        /// </summary>
        [JsonPropertyName("maxLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the regex pattern for string properties.
        /// </summary>
        [JsonPropertyName("pattern")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets the format for string properties (e.g., "date-time", "email", "uri").
        /// </summary>
        [JsonPropertyName("format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Format { get; set; }
    }
}
