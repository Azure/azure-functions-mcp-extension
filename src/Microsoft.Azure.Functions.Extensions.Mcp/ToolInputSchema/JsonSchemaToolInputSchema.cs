// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

/// <summary>
/// Validates tool request arguments using a JSON schema.
/// </summary>
internal sealed class JsonSchemaToolInputSchema : ToolInputSchema, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the JsonSchemaToolInputSchema class.
    /// </summary>
    /// <param name="inputSchema">The JSON schema to use for validation.</param>
    public JsonSchemaToolInputSchema(JsonDocument inputSchema)
    {
        InputSchema = inputSchema ?? throw new ArgumentNullException(nameof(inputSchema));
    }

    public void Dispose()
    {
        InputSchema?.Dispose();
    }

    /// <summary>
    /// Gets the list of required property names for validation from the JSON schema.
    /// </summary>
    /// <returns>A collection of required property names.</returns>
    protected override IReadOnlyCollection<string> GetRequiredProperties()
    {
        ArgumentNullException.ThrowIfNull(InputSchema);
        return McpInputSchemaJsonUtilities.GetRequiredProperties(InputSchema);
    }
}
