// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpUseResultSchemaTransformer : IFunctionMetadataTransformer
{
    public string Name => nameof(McpUseResultSchemaTransformer);

    private const string McpToolTriggerBindingType = "mcpToolTrigger";

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

                // Check binding type
                if (jsonObject.TryGetPropertyValue("type", out var typeNode))
                {
                    var bindingType = typeNode?.ToString();
                    if (string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase))
                    {
                        jsonObject["useResultSchema"] = true;
                        function.RawBindings[i] = jsonObject.ToJsonString();
                    }
                }
            }
        }
    }
}
