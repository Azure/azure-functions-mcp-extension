// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Marks a POCO type as an MCP output type.
/// Types decorated with this attribute are serialized as structured content,
/// and their JSON schema is automatically generated and included in the tool definition.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="McpContentAttribute"/> when you only want structured content serialization.
/// Use <see cref="McpOutputAttribute"/> when you also want the output schema advertised
/// to clients during <c>tools/list</c>.
/// </para>
/// <para>
/// This attribute can be applied to classes, structs, and record types.
/// An explicit <c>WithOutputSchema</c> configuration via the fluent API takes precedence
/// over the auto-generated schema.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class McpOutputAttribute : Attribute
{
}
