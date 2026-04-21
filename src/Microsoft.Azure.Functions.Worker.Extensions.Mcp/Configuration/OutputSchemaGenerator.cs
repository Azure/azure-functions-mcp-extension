// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Generates JSON output schemas from function return types using reflection.
/// The return type (unwrapped from <see cref="Task{T}"/>) must be decorated with
/// <see cref="McpOutputAttribute"/> for auto-generation.
/// </summary>
internal static class OutputSchemaGenerator
{
    /// <summary>
    /// Attempts to generate a JSON output schema from the function's return type.
    /// Returns true if the return type is decorated with <see cref="McpOutputAttribute"/>
    /// and a valid schema was generated.
    /// </summary>
    public static bool TryGenerateFromFunction(IFunctionMetadata functionMetadata, out JsonNode? outputSchema, ILogger? logger = null)
    {
        outputSchema = null;

        try
        {
            if (!FunctionMethodResolver.TryGetScriptRoot(out _))
            {
                return false;
            }

            if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method) || method is null)
            {
                return false;
            }

            var returnType = UnwrapReturnType(method.ReturnType);

            if (returnType is null || !HasMcpOutputAttribute(returnType))
            {
                return false;
            }

            outputSchema = GenerateSchemaFromType(returnType);
            return outputSchema is not null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to generate output schema from function '{FunctionName}' via reflection.", functionMetadata.Name);
            return false;
        }
    }

    /// <summary>
    /// Unwraps the inner type from Task{T} or ValueTask{T}, returning the actual result type.
    /// Returns null for void, Task, or ValueTask (no result type).
    /// </summary>
    internal static Type? UnwrapReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return null;
        }

        if (returnType.IsGenericType)
        {
            var genericDef = returnType.GetGenericTypeDefinition();
            if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
            {
                return returnType.GetGenericArguments()[0];
            }
        }

        return returnType;
    }

    /// <summary>
    /// Checks whether the type is decorated with <see cref="McpOutputAttribute"/>.
    /// </summary>
    private static bool HasMcpOutputAttribute(Type type)
    {
        return type.GetCustomAttribute<McpOutputAttribute>(inherit: true) is not null;
    }

    /// <summary>
    /// Generates a JSON schema from the specified type using <see cref="JsonSchemaExporter"/>.
    /// </summary>
    internal static JsonNode? GenerateSchemaFromType(Type type)
    {
        var options = JsonSerializerOptions.Default;
        var schemaNode = options.GetJsonSchemaAsNode(type, new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
        });

        return schemaNode;
    }
}
