// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents resource configuration options for metadata.
/// </summary>
public class ResourceOptions
{
    /// <summary>
    /// Gets or sets the metadata associated with the resource.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];

    /// <summary>
    /// Adds a metadata entry with the specified key and value.
    /// </summary>
    /// <param name="key">The key for the metadata entry. Cannot be null or empty.</param>
    /// <param name="value">The value for the metadata entry.</param>
    public void AddMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        Metadata[key] = value;
    }
}
