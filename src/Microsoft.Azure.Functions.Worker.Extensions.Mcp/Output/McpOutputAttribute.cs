// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Attribute to mark a method that returns structured content with a specific output schema.
/// This attribute should be applied to methods to define the structure of their output for MCP tools.
/// Use this when you want to return structured content (either alone or alongside content blocks).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class McpOutputAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpOutputAttribute"/> class.
    /// </summary>
    /// <param name="outputType">The type that defines the structure of the output. 
    /// This should be a POCO type whose properties will be used to generate the JSON schema.</param>
    public McpOutputAttribute(Type outputType)
    {
        ArgumentNullException.ThrowIfNull(outputType, nameof(outputType));
        OutputType = outputType;
    }

    /// <summary>
    /// Gets the type that defines the structure of the output schema.
    /// </summary>
    public Type OutputType { get; }
}
