// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.BindingTypeResolver;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpFunctionMetadataTransformer(IOptionsMonitor<ToolOptions> toolOptionsMonitor, ILogger<McpFunctionMetadataTransformer> logger)
    : IFunctionMetadataTransformer
{
    private readonly IInputSchemaResolver _inputSchemaResolver = new CompositeInputSchemaResolver(
        new ExplicitInputSchemaResolver(),
        new PropertyBasedInputSchemaResolver(),
        new ReflectionBasedInputSchemaResolver(logger));

    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        foreach (var function in original)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            JsonNode? inputSchema = null;
            Dictionary<string, ToolPropertyBinding> inputBindingProperties = [];

            for (int i = 0; i < function.RawBindings.Count; i++)
            {
                var binding = function.RawBindings[i];
                var node = JsonNode.Parse(binding);

                if (node is not JsonObject jsonObject
                    || !jsonObject.TryGetPropertyValue("type", out var bindingTypeNode))
                {
                    continue;
                }

                var bindingType = bindingTypeNode?.ToString();

                switch (bindingType)
                {
                    case McpToolTriggerBindingType:
                        ProcessToolTriggerBinding(jsonObject, function, ref inputSchema);
                        function.RawBindings[i] = jsonObject.ToJsonString();
                        break;

                    case McpResourceTriggerBindingType:
                        if (MetadataParser.TryGetResourceMetadata(function, out var resourceMetadataJson, logger))
                        {
                            jsonObject["metadata"] = resourceMetadataJson;
                            function.RawBindings[i] = jsonObject.ToJsonString();
                        }
                        break;

                    case McpToolPropertyBindingType:
                        if (jsonObject.TryGetPropertyValue(McpToolPropertyName, out var propertyNameNode)
                            && propertyNameNode is not null)
                        {
                            var propertyName = propertyNameNode.ToString();
                            inputBindingProperties.TryAdd(propertyName, new ToolPropertyBinding(i, jsonObject));
                        }
                        break;
                }
            }

            // Patch input binding metadata with type information
            PatchInputBindingMetadata(function, inputBindingProperties, inputSchema);
        }
    }

    private void ProcessToolTriggerBinding(
        JsonObject jsonObject,
        IFunctionMetadata function,
        ref JsonNode? inputSchema)
    {
        if (!jsonObject.TryGetPropertyValue("toolName", out var toolNameNode))
        {
            return;
        }

        var toolName = toolNameNode?.ToString();
        jsonObject["useWorkerInputSchema"] = true;

        TryResolveInputSchema(toolName, function, jsonObject, out inputSchema);

        if (MetadataParser.TryGetToolMetadata(function, out var toolMetadataJson, logger))
        {
            jsonObject["metadata"] = toolMetadataJson;
        }
    }

    private bool TryResolveInputSchema(
        string? toolName,
        IFunctionMetadata function,
        JsonObject jsonObject,
        out JsonNode? inputSchema)
    {
        inputSchema = null;

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (_inputSchemaResolver.TryResolve(toolOptions, function, out inputSchema) && inputSchema is not null)
        {
            jsonObject["inputSchema"] = inputSchema.ToJsonString();
            return true;
        }

        inputSchema = null;
        return false;
    }

    private static void PatchInputBindingMetadata(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties,
        JsonNode? inputSchema)
    {
        if (inputBindingProperties.Count == 0 || inputSchema is null)
        {
            return;
        }

        ResolveAndApplyTypes(inputSchema, inputBindingProperties);
        UpdateRawBindings(function, inputBindingProperties);
    }

    private static void UpdateRawBindings(
        IFunctionMetadata function,
        Dictionary<string, ToolPropertyBinding> inputBindingProperties)
    {
        foreach (var (_, binding) in inputBindingProperties)
        {
            function.RawBindings![binding.Index] = binding.Binding.ToJsonString();
        }
    }
}
