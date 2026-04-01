// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents a prompt argument definition for serialization to the host binding metadata.
/// </summary>
public sealed class PromptArgumentDefinition(
    string name,
    string? description = null,
    bool isRequired = false)
{
    /// <summary>
    /// Gets the name of the prompt argument.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    /// <summary>
    /// Gets the description of the prompt argument.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; } = description;

    /// <summary>
    /// Gets a value indicating whether the prompt argument is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; } = isRequired;
}
