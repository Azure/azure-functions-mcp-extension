// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public class ToolProperty(string name, string type, string? description, bool required = false)
{
    [JsonPropertyName("propertyName")]
    public string Name { get; set; } = name;

    [JsonPropertyName("propertyType")]
    public string Type { get; set; } = type;

    [JsonPropertyName("description")]
    public string? Description { get; set; } = description;

    [JsonPropertyName("required")]
    public bool Required { get; set; } = required;
}