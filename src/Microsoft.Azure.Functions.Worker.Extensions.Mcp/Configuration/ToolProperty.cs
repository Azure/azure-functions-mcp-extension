// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class ToolProperty(string name, string type, string? description,
                                 bool required = false, bool isArray = false)
{
    [JsonPropertyName("propertyName")]
    public string Name { get; init; } = name;

    [JsonPropertyName("propertyType")]
    public string Type { get; init; } = type;

    [JsonPropertyName("description")]
    public string? Description { get; init; } = description;

    [JsonPropertyName("required")]
    public bool Required { get; init; } = required;

    [JsonPropertyName("isArray")]
    public bool IsArray { get; init; } = isArray;
}
