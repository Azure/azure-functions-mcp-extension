// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to add metadata to an MCP trigger.
/// This is a declarative attribute read during registration, not an input binding.
/// </summary>
/// <remarks>
/// <para>
/// Provide metadata as a JSON string. For example:
/// </para>
/// <code>
/// [McpMetadata("{\"author\":\"John Doe\",\"version\":1.0}")]
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [McpMetadata("{\"author\":\"John\",\"tags\":[\"utility\",\"time\"]}")]
/// [McpMetadata("{\"ui\":{\"resourceUri\":\"ui://my-app/widget\",\"prefersBorder\":true}}")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class McpMetadataAttribute(string json) : Attribute
{
    /// <summary>
    /// Gets the metadata as a JSON string.
    /// </summary>
    public string Json { get; } = json;
}
