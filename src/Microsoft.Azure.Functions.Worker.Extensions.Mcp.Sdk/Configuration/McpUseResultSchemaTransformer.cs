// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpUseResultSchemaTransformer : IFunctionMetadataTransformer
{
    public string Name => nameof(McpUseResultSchemaTransformer);
    private const string McpToolTriggerBindingType = "mcpToolTrigger";
    private const string UseResultSchemaFlag = "useResultSchema";

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

            // Check if there are any output bindings
            bool hasOutputBindings = HasOutputBindings(function.RawBindings);

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

                // Check binding type
                if (jsonObject.TryGetPropertyValue("type", out var typeNode))
                {
                    var bindingType = typeNode?.ToString();
                    if (string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Only set useResultSchema to true if there are NO output bindings
                        // When output bindings exist, we want SimpleToolReturnValueBinder to handle the raw value
                        if (!hasOutputBindings)
                        {
                            jsonObject[UseResultSchemaFlag] = true;
                        }
                        function.RawBindings[i] = jsonObject.ToJsonString();
                    }
                }
            }
        }
    }

    private static bool HasOutputBindings(IList<string> rawBindings)
    {
        foreach (var bindingJson in rawBindings)
        {
            if (string.IsNullOrWhiteSpace(bindingJson))
            {
                continue;
            }

            var node = JsonNode.Parse(bindingJson);
            if (node is not JsonObject jsonObject)
            {
                continue;
            }

            // Check if direction is "out"
            if (jsonObject.TryGetPropertyValue("direction", out var directionNode))
            {
                var direction = directionNode?.ToString();
                if (string.Equals(direction, "out", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
