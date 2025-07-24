// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using Microsoft.Extensions.Options;

//[assembly: WorkerExtensionStartup(typeof(McpExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed class McpFunctionMetadataProvider(IFunctionMetadataProvider inner, IOptionsSnapshot<ToolOptions> toolOptionsSnapshot)
    : IFunctionMetadataProvider
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    private static readonly Regex EntryPointRegex = new(@"^(?<typename>.*)\.(?<methodname>\S*)$");

    public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
    {
        var metadata = await inner.GetFunctionMetadataAsync(directory);

        foreach (var function in metadata)
        {
            if (function.RawBindings is null
                || function.Name is null)
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

                var node = JsonNode.Parse(binding);

                if (node is not JsonObject jsonObject)
                {
                    continue;
                }

                if (jsonObject.TryGetPropertyValue("type", out var triggerType)
                    && triggerType?.ToString() == "mcpToolTrigger"
                    && jsonObject.TryGetPropertyValue("toolName", out var toolName)
                    && GetToolProperties(toolName?.ToString(), function, out var toolProperties))
                {
                    jsonObject["toolProperties"] = GetPropertiesJson(function.Name, toolProperties);

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
        var toolOptions = toolOptionsSnapshot.Get(toolName);

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
            if (GetToolPropertyFromToolPropertyAttribute(parameter) is ToolProperty toolProperty)
            {
                properties.Add(toolProperty);
                continue;
            }

            properties.AddRange(GetToolPropertiesFromToolTriggerAttribute(parameter));
        }

        toolProperties = properties;
        return true;
    }

    private static JsonNode? GetPropertiesJson(string functionName, List<ToolProperty> properties)
    {
        return JsonSerializer.Serialize(properties);
    }

    private static ToolProperty? GetToolPropertyFromToolPropertyAttribute(ParameterInfo parameter)
    {
        var toolAttribute = parameter.GetCustomAttribute<McpToolPropertyAttribute>();
        if (toolAttribute is null)
        {
            return null;
        }

        return new ToolProperty(
            toolAttribute.PropertyName,
            toolAttribute.PropertyType,
            toolAttribute.Description,
            toolAttribute.Required);
    }

    private static IEnumerable<ToolProperty> GetToolPropertiesFromToolTriggerAttribute(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<McpToolTriggerAttribute>() is null
            || !parameter.ParameterType.IsPoco()
            || parameter.ParameterType == typeof(ToolInvocationContext))
        {
            yield break;
        }

        foreach (var property in parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            yield return new ToolProperty(
                name:        property.Name,
                type:        property.PropertyType.MapToToolPropertyType(),
                description: property.GetDescription(),
                required:    property.IsRequired()
            );
        }
    }
}
