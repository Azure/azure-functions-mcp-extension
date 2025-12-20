// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Abstract base class for validating tool request arguments and providing schema information.
/// </summary>
internal abstract class ToolInputSchema
{
    /// <summary>
    /// Validates that all required arguments are present in the tool call request.
    /// </summary>
    /// <param name="callToolRequest">The tool call request parameters to validate.</param>
    /// <exception cref="McpProtocolException">Thrown when required properties are missing.</exception>
    public void Validate(CallToolRequestParams? callToolRequest)
    {
        var requiredProperties = GetRequiredProperties();
        
        if (requiredProperties.Count == 0)
        {
            return;
        }

        var missing = new List<string>();
        var args = callToolRequest?.Arguments;

        foreach (var propertyName in requiredProperties)
        {
            if (args == null
                || !args.TryGetValue(propertyName, out var value)
                || IsValueNullOrUndefined(value))
            {
                missing.Add(propertyName);
            }
        }

        if (missing.Count > 0)
        {
            throw CreateValidationException(missing);
        }
    }

    /// <summary>
    /// Gets a JsonElement representing the complete input schema for this tool.
    /// This combines both JSON schema documents and property-based schemas into a unified format.
    /// </summary>
    /// <returns>A JsonElement representing the input schema.</returns>
    public abstract JsonElement GetSchemaElement();

    /// <summary>
    /// Gets the list of required property names for validation.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected abstract IReadOnlyCollection<string> GetRequiredProperties();

    /// <summary>
    /// Gets or sets the properties of the tool properties if useWorkerInputSchema is false.
    /// </summary>
    public ICollection<IMcpToolProperty> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the properties of the input schema if useWorkerInputSchema is true.
    /// </summary>
    public JsonDocument? InputSchema { get; set; }

    /// <summary>
    /// Checks if a JSON element represents a null or undefined value.
    /// </summary>
    /// <param name="value">The JSON element to check.</param>
    /// <returns>True if the value is null or undefined; otherwise false.</returns>
    protected static bool IsValueNullOrUndefined(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Undefined => true,
            _ => false
        };
    }

    /// <summary>
    /// Creates a validation exception for missing required properties.
    /// </summary>
    /// <param name="missingProperties">The list of missing property names.</param>
    /// <returns>An McpProtocolException with details about the missing properties.</returns>
    protected static McpProtocolException CreateValidationException(IEnumerable<string> missingProperties)
    {
        var missing = string.Join(", ", missingProperties);
        return new McpProtocolException(
            $"One or more required tool properties are missing values. Please provide: {missing}", 
            McpErrorCode.InvalidParams);
    }
}
