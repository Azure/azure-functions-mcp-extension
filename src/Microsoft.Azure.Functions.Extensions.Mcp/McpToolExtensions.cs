// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class McpToolExtensions
{
    internal static JsonElement GetPropertiesInputSchema(this IMcpTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        return tool.ToolInputSchema.GetSchemaElement();
    }

    private static bool IsDefaultSchema(JsonElement schema)
    {
        // Check if this is the default empty schema
        if (!schema.TryGetProperty("properties", out var properties) ||
            !schema.TryGetProperty("required", out var required))
        {
            return false;
        }

        // Default schema has empty properties and required arrays
        return properties.ValueKind == JsonValueKind.Object && 
               properties.EnumerateObject().Count() == 0 &&
               required.ValueKind == JsonValueKind.Array && 
               required.GetArrayLength() == 0;
    }
}
