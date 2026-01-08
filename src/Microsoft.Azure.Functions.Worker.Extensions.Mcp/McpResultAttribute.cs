// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Marks a POCO class as an MCP result type that should be serialized as structured content.
/// When a function returns an object of a type decorated with this attribute,
/// the result will be serialized as both text content (for backwards compatibility)
/// and structured content (for clients that support it).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class McpResultAttribute : Attribute
{
}
