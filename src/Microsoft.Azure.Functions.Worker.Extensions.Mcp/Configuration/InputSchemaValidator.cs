// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Validates that a JSON schema conforms to MCP tool input schema requirements.
/// </summary>
internal static class InputSchemaValidator
{
    public static void Validate(JsonNode schemaNode, Type? sourceType = null)
    {
        if (schemaNode is not JsonObject schemaObject)
        {
            throw new ArgumentException(
                FormatError("Input schema must be a JSON object.", sourceType));
        }

        if (!schemaObject.TryGetPropertyValue("type", out var typeNode)
            || typeNode?.GetValue<string>() != "object")
        {
            throw new ArgumentException(
                FormatError(
                    "Input schema must have root \"type\": \"object\". " +
                    "Ensure you are passing a class or record type with public properties, not a primitive type.",
                    sourceType));
        }

        if (schemaObject.TryGetPropertyValue("properties", out var propsNode)
            && propsNode is not null && propsNode is not JsonObject)
        {
            throw new ArgumentException(
                FormatError("Input schema \"properties\" must be a JSON object.", sourceType));
        }

        if (schemaObject.TryGetPropertyValue("required", out var reqNode)
            && reqNode is not null && reqNode is not JsonArray)
        {
            throw new ArgumentException(
                FormatError("Input schema \"required\" must be a JSON array.", sourceType));
        }
    }

    private static string FormatError(string message, Type? sourceType)
    {
        return sourceType is not null
            ? $"{message} Type: '{sourceType.FullName}'."
            : message;
    }
}
