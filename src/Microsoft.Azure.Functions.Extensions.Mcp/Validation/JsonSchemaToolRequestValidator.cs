// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates tool request arguments using a JSON schema.
/// </summary>
internal sealed class JsonSchemaToolRequestValidator : ToolRequestValidator
{
    private readonly JsonDocument _inputSchema;

    /// <summary>
    /// Initializes a new instance of the JsonSchemaToolRequestValidator class.
    /// </summary>
    /// <param name="inputSchema">The JSON schema to use for validation.</param>
    public JsonSchemaToolRequestValidator(JsonDocument inputSchema)
    {
        _inputSchema = inputSchema ?? throw new ArgumentNullException(nameof(inputSchema));
    }

    /// <summary>
    /// Gets the list of required property names for validation from the JSON schema.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected override IReadOnlyCollection<string> GetRequiredProperties()
    {
        return McpInputSchemaJsonUtilities.GetRequiredProperties(_inputSchema);
    }
}
