// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates tool request arguments using a JSON schema.
/// </summary>
internal sealed class JsonSchemaToolInputSchema : ToolInputSchema, IDisposable
{
    private readonly JsonDocument _inputSchema;
    private readonly Lazy<JsonElement> _cachedSchemaElement;

    /// <summary>
    /// Initializes a new instance of the JsonSchemaToolInputSchema class.
    /// </summary>
    /// <param name="inputSchema">The JSON schema to use for validation.</param>
    public JsonSchemaToolInputSchema(JsonDocument inputSchema)
    {
        _inputSchema = inputSchema ?? throw new ArgumentNullException(nameof(inputSchema));
        _cachedSchemaElement = new Lazy<JsonElement>(() => _inputSchema.RootElement);
    }

    /// <summary>
    /// Gets a JsonElement representing the complete input schema for this tool.
    /// Returns the JSON schema document directly.
    /// </summary>
    /// <returns>A JsonElement representing the input schema.</returns>
    public override JsonElement GetSchemaElement()
    {
        return _cachedSchemaElement.Value;
    }

    /// <summary>
    /// Gets the list of required property names for validation from the JSON schema.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected override IReadOnlyCollection<string> GetRequiredProperties()
    {
        return McpInputSchemaJsonUtilities.GetRequiredProperties(_inputSchema);
    }

    /// <summary>
    /// Disposes the JSON schema document.
    /// </summary>
    public void Dispose()
    {
        _inputSchema?.Dispose();
    }
}
