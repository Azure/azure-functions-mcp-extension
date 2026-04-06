// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;

/// <summary>
/// Fluent builder that enriches a function's MCP bindings step by step.
/// Add new transformation methods via extension methods in separate files —
/// the builder itself never needs to change.
/// </summary>
internal sealed class McpBindingBuilder
{
    internal McpBuilderContext Context { get; }

    public McpBindingBuilder(IFunctionMetadata function, ILogger logger, HashSet<string>? sharedEmittedAppTools = null)
    {
        var bindings = ParseBindings(function);
        Context = new McpBuilderContext(function, bindings, logger, sharedEmittedAppTools);
    }

    public bool HasBindings => Context.Bindings.Count > 0;

    public void Build()
    {
        foreach (var binding in Context.Bindings)
        {
            Context.Function.RawBindings![binding.Index] = binding.JsonObject.ToJsonString();
        }
    }

    private static List<McpParsedBinding> ParseBindings(IFunctionMetadata function)
    {
        var bindings = new List<McpParsedBinding>();

        for (int i = 0; i < function.RawBindings!.Count; i++)
        {
            var node = JsonNode.Parse(function.RawBindings[i]);

            if (node is not JsonObject jsonObject
                || !jsonObject.TryGetPropertyValue("type", out var bindingTypeNode))
            {
                continue;
            }

            var bindingType = bindingTypeNode?.ToString();

            string? identifier;
            switch (bindingType)
            {
                case McpToolTriggerBindingType:
                    identifier = jsonObject["toolName"]?.ToString();
                    break;
                case McpResourceTriggerBindingType:
                    identifier = jsonObject["uri"]?.ToString();
                    break;
                case McpPromptTriggerBindingType:
                    identifier = jsonObject["promptName"]?.ToString();
                    break;
                case McpToolPropertyBindingType:
                    identifier = jsonObject[McpToolPropertyName]?.ToString();
                    break;
                case McpPromptArgumentBindingType:
                    identifier = jsonObject[McpPromptArgumentName]?.ToString();
                    break;
                default:
                    continue;
            }

            bindings.Add(new McpParsedBinding(i, bindingType!, identifier, jsonObject));
        }

        return bindings;
    }
}
