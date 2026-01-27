// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to add metadata to an MCP trigger.
/// Can be applied multiple times to add multiple metadata key-value pairs.
/// This is a declarative attribute read during registration, not an input binding.
/// </summary>
/// <remarks>
/// <para>
/// Supports colon notation for nested metadata paths. For example:
/// </para>
/// <code>
/// [McpMetadata("author", "John Doe")]                    // { "author": "John Doe" }
/// [McpMetadata("ui:resourceUri", "ui://my-app/widget")]  // { "ui": { "resourceUri": "ui://my-app/widget" } }
/// [McpMetadata("ui:prefersBorder", "true")]              // Merges into ui object
/// </code>
/// <para>
/// Multiple attributes with the same nested path prefix will be merged into a single object.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [McpMetadata("ui:resourceUri", "ui://time/widget.html")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class McpMetadataAttribute(string key, string? value) : Attribute, IKeyValueMetadataAttribute
{
    /// <summary>
    /// Gets the metadata key. Use colon notation (e.g., "ui:resourceUri") for nested paths.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    public string? Value { get; } = value;

    object? IKeyValueMetadataAttribute.Value => Value;
}
