// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Injects output schemas into MCP tool trigger bindings.
/// Handles two sources of output schema, with the following priority:
/// <list type="number">
///   <item><description>
///     <b>Explicit schema</b> — configured via the fluent API (<c>WithOutputSchema</c>)
///     and stored in <see cref="ToolOptions.OutputSchema"/>.
///   </description></item>
///   <item><description>
///     <b>Auto-generated schema</b> — inferred when the function's return type is
///     decorated with <see cref="McpOutputAttribute"/>. The schema is generated
///     from the CLR type using <see cref="JsonSchemaExporter"/>.
///   </description></item>
/// </list>
/// </summary>
internal sealed class McpOutputSchemaTransformer(
    IFunctionMethodResolver functionMethodResolver,
    IOptionsMonitor<ToolOptions> toolOptionsMonitor,
    ILogger<McpOutputSchemaTransformer> logger)
    : IFunctionMetadataTransformer
{
    public string Name => nameof(McpOutputSchemaTransformer);

    private const string McpToolTriggerBindingType = "mcpToolTrigger";
    private const string OutputSchemaKey = "outputSchema";

    public void Transform(IList<IFunctionMetadata> original)
    {
        if (original is null || original.Count == 0)
        {
            return;
        }

        foreach (var function in original)
        {
            if (function.RawBindings is null || function.RawBindings.Count == 0)
            {
                continue;
            }

            for (int i = 0; i < function.RawBindings.Count; i++)
            {
                var bindingJson = function.RawBindings[i];
                if (string.IsNullOrWhiteSpace(bindingJson))
                {
                    continue;
                }

                var node = JsonNode.Parse(bindingJson);
                if (node is not JsonObject jsonObject)
                {
                    continue;
                }

                if (!jsonObject.TryGetPropertyValue("type", out var typeNode))
                {
                    continue;
                }

                var bindingType = typeNode?.ToString();
                if (!string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryInjectOutputSchema(jsonObject, function))
                {
                    function.RawBindings[i] = jsonObject.ToJsonString();
                }
            }
        }
    }

    private bool TryInjectOutputSchema(JsonObject jsonObject, IFunctionMetadata function)
    {
        if (!jsonObject.TryGetPropertyValue("toolName", out var toolNameNode)
            || toolNameNode is null)
        {
            return false;
        }

        var toolName = toolNameNode.ToString();
        var toolOptions = toolOptionsMonitor.Get(toolName);
        var hasExplicitSchema = !string.IsNullOrWhiteSpace(toolOptions.OutputSchema);
        var hasOutputAttribute = HasMcpOutputReturnType(function);

        if (hasExplicitSchema && hasOutputAttribute)
        {
            logger.LogWarning(
                "Tool '{ToolName}' in function '{FunctionName}' has both an explicit output schema " +
                "(via WithOutputSchema) and an [McpOutput] return type. The explicit schema will take precedence.",
                toolName,
                function.Name);
        }

        // Priority 1: Explicit output schema from fluent API.
        if (hasExplicitSchema)
        {
            jsonObject[OutputSchemaKey] = toolOptions.OutputSchema;
            return true;
        }

        // Priority 2: Auto-generate from [McpOutput] return type.
        return TryInjectAutoOutputSchema(jsonObject, function);
    }

    private bool HasMcpOutputReturnType(IFunctionMetadata function)
    {
        if (!functionMethodResolver.TryResolveMethod(function, out var method) || method is null)
        {
            return false;
        }

        var returnType = UnwrapReturnType(method.ReturnType);
        return returnType is not null && HasMcpOutputAttribute(returnType);
    }

    private bool TryInjectAutoOutputSchema(JsonObject jsonObject, IFunctionMetadata function)
    {
        if (!functionMethodResolver.TryResolveMethod(function, out var method) || method is null)
        {
            return false;
        }

        var returnType = UnwrapReturnType(method.ReturnType);
        if (returnType is null || !HasMcpOutputAttribute(returnType))
        {
            return false;
        }

        try
        {
            // Use Web defaults which includes camelCase naming policy
            // to match the property names produced when serializing structured content.
            var schemaNode = SchemaExporterOptionsFactory.DefaultSerializerOptions.GetJsonSchemaAsNode(
                returnType,
                SchemaExporterOptionsFactory.Create());

            jsonObject[OutputSchemaKey] = schemaNode.ToJsonString();

            logger.LogDebug(
                "Auto-generated output schema for tool in function '{FunctionName}' from return type '{ReturnType}'.",
                function.Name,
                returnType.FullName);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to auto-generate output schema for tool in function '{FunctionName}' from return type '{ReturnType}'.",
                function.Name,
                returnType.FullName);
            return false;
        }
    }

    /// <summary>
    /// Unwraps async return types to get the inner type.
    /// For <c>Task&lt;T&gt;</c> and <c>ValueTask&lt;T&gt;</c>, returns <c>T</c>.
    /// For <c>Task</c>, <c>ValueTask</c>, <c>void</c>, returns null (no meaningful return type).
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
    /// Checks if the type is directly decorated with <see cref="McpOutputAttribute"/>.
    /// </summary>
    private static bool HasMcpOutputAttribute(Type type)
    {
        return type.GetCustomAttributes(typeof(McpOutputAttribute), inherit: false).Length > 0;
    }
}
