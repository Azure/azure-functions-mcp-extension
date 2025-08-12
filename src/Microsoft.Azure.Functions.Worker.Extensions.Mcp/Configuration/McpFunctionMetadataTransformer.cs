﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpFunctionMetadataTransformer(IOptionsMonitor<ToolOptions> toolOptionsMonitor)
    : IFunctionMetadataTransformer
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    private static readonly Regex EntryPointRegex = new(@"^(?<typename>.*)\.(?<methodname>\S*)$");

    public ImmutableArray<IFunctionMetadata> Transform(ImmutableArray<IFunctionMetadata> metadata)
    {
        foreach (var function in metadata)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            for (int i = 0; i < function.RawBindings.Count; i++)
            {
                var binding = function.RawBindings[i];
                if (!binding.Contains("mcpToolTrigger"))
                {
                    continue;
                }

                if (JsonNode.Parse(binding) is not JsonObject jsonObject)
                {
                    continue;
                }

                if (jsonObject.TryGetPropertyValue("type", out var typeNode)
                    && typeNode?.ToString() == "mcpToolTrigger"
                    && jsonObject.TryGetPropertyValue("toolName", out var nameNode)
                    && GetToolProperties(nameNode?.ToString(), function, out var props))
                {
                    jsonObject["toolProperties"] = JsonSerializer.Serialize(props);
                    function.RawBindings[i] = jsonObject.ToJsonString();
                    break;
                }
            }
        }

        return metadata;
    }

    private bool GetToolProperties(string? toolName, IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<ToolProperty>? toolProperties)
    {
        toolProperties = null;

        // Get from configured options first:
        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.Properties.Count != 0)
        {
            toolProperties = toolOptions.Properties;
            return true;
        }

        return TryGetPropertiesFromAttributes(functionMetadata, ref toolProperties);
    }

    private static bool TryGetPropertiesFromAttributes(IFunctionMetadata functionMetadata, ref List<ToolProperty>? toolProperties)
    {
        // Fallback to attributes:
        var match = EntryPointRegex.Match(functionMetadata.EntryPoint ?? string.Empty);

        if (!match.Success)
        {
            return false;
        }

        var typeName = match.Groups["typename"].Value;
        var methodName = match.Groups["methodname"].Value;

        var scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
                        ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

        if (string.IsNullOrWhiteSpace(scriptRoot))
        {
            throw new InvalidOperationException($"The '{FunctionsApplicationDirectoryKey}' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
        }

        var scriptFile = Path.Combine(scriptRoot, functionMetadata.ScriptFile ?? string.Empty);
        var assemblyPath = Path.GetFullPath(scriptFile);
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType(typeName);

        if (type is null)
        {
            return false;
        }

        var method = type.GetMethod(methodName);

        if (method is null)
        {
            return false;
        }

        var properties = new List<ToolProperty>();

        foreach (var parameter in method.GetParameters())
        {
            if (TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty))
            {
                properties.Add(toolProperty);
            }
            else if (TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var triggerProperties))
            {
                properties.AddRange(triggerProperties);
            }
        }

        toolProperties = properties;
        return true;
    }

    private static JsonNode? GetPropertiesJson(string functionName, List<ToolProperty> properties)
    {
        return JsonSerializer.Serialize(properties);
    }

    private static bool TryGetToolPropertyFromToolPropertyAttribute(ParameterInfo parameter, out ToolProperty toolProperty)
    {
        var toolAttribute = parameter.GetCustomAttribute<McpToolPropertyAttribute>();
        if (toolAttribute is null)
        {
            toolProperty = default!;
            return false;
        }

        toolProperty = new ToolProperty(
            toolAttribute.PropertyName,
            toolAttribute.PropertyType,
            toolAttribute.Description,
            toolAttribute.Required);

        return true;
    }

    private static bool TryGetToolPropertiesFromToolTriggerAttribute(ParameterInfo parameter, out List<ToolProperty> toolProperties)
    {
        toolProperties = new List<ToolProperty>();

        var triggerAttribute = parameter.GetCustomAttribute<McpToolTriggerAttribute>();
        if (triggerAttribute is null
            || !parameter.ParameterType.IsPoco()
            || parameter.ParameterType == typeof(ToolInvocationContext))
        {
            return false;
        }

        foreach (var property in parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            toolProperties.Add(new ToolProperty(
                property.Name,
                property.PropertyType.MapToToolPropertyType(),
                property.GetDescription(),
                property.IsRequired()
            ));
        }

        return toolProperties.Count > 0;
    }
}
