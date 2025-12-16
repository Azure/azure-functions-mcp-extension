// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to add metadata to an MCP Resource.
/// Can be applied multiple times to add multiple metadata key-value pairs.
/// This is a declarative attribute read during resource registration, not an input binding.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class McpResourceMetadataAttribute(string key, object? value) : Attribute
{
    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    public object? Value { get; } = value;
}
