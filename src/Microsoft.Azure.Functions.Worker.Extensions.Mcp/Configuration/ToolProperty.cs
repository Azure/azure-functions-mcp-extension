// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class ToolProperty(string name, string type, string? description,
                                 bool isRequired = false, bool isArray = false)
{
    [JsonPropertyName("propertyName")]
    public string Name { get; init; } = name;

    [JsonPropertyName("propertyType")]
    public string Type { get; init; } = type;

    [JsonPropertyName("description")]
    public string? Description { get; init; } = description;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; } = isRequired;

    [JsonPropertyName("isArray")]
    public bool IsArray { get; init; } = isArray;
}
