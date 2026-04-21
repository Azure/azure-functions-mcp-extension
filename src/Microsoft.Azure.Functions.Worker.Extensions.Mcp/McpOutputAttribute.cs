// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Marks a POCO return type for automatic output schema generation.
/// When a function's return type (or its inner type for <see cref="System.Threading.Tasks.Task{T}"/>)
/// is decorated with this attribute, the MCP extension will auto-generate a JSON Schema
/// from the type and include it as the tool's output schema.
/// </summary>
/// <remarks>
/// This attribute can be applied to classes, structs, and record types.
/// The generated schema will have root <c>"type": "object"</c> and include all public properties.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class McpOutputAttribute : Attribute
{
}
