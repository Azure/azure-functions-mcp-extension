// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Extracts prompt arguments from function parameters and attributes.
/// </summary>
internal static class PromptArgumentParser
{
    /// <summary>
    /// Attempts to get prompt arguments from function attributes.
    /// </summary>
    public static bool TryGetArgumentsFromAttributes(IFunctionMetadata functionMetadata, out List<PromptArgumentDefinition>? arguments)
    {
        FunctionMethodResolver.EnsureScriptRoot();

        if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method))
        {
            arguments = null;
            return false;
        }

        var parameters = method!.GetParameters();
        var result = new List<PromptArgumentDefinition>(capacity: parameters.Length);

        foreach (var parameter in parameters)
        {
            var attr = parameter.GetCustomAttribute<McpPromptArgumentAttribute>();
            if (attr is null)
            {
                continue;
            }

            result.Add(new PromptArgumentDefinition(attr.ArgumentName, attr.Description, attr.IsRequired));
        }

        arguments = result;
        return result.Count > 0;
    }

    /// <summary>
    /// Serializes prompt arguments to a JSON node.
    /// </summary>
    public static JsonNode? GetArgumentsJson(List<PromptArgumentDefinition> arguments)
    {
        return JsonSerializer.Serialize(arguments);
    }
}
