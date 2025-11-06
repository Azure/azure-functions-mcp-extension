// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Constants;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public sealed partial class McpFunctionMetadataTransformer(IOptionsMonitor<ToolOptions> toolOptionsMonitor)
    : IFunctionMetadataTransformer
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    public string Name => nameof(McpFunctionMetadataTransformer);

    public void Transform(IList<IFunctionMetadata> original)
    {
        foreach (var function in original)
        {
            if (function.RawBindings is null || function.Name is null)
            {
                continue;
            }

            List<ToolProperty>? toolProperties = null;
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

                if (string.Equals(bindingType, McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue("toolName", out var toolNameNode)
                    && GetToolProperties(toolNameNode?.ToString(), function, out toolProperties))
                {
                    jsonObject["toolProperties"] = GetPropertiesJson(function.Name, toolProperties);
                    function.RawBindings[i] = jsonObject.ToJsonString();
                }
                else if (string.Equals(bindingType, McpToolPropertyBindingType, StringComparison.OrdinalIgnoreCase)
                    && jsonObject.TryGetPropertyValue(McpToolPropertyName, out var propertyNameNode)
                    && propertyNameNode is not null)
                {
                    var propertyName = propertyNameNode.ToString();
                    inputBindingProperties.TryAdd(propertyName, new ToolPropertyBinding(i, jsonObject));
                }
            }

            // This is required for attributed properties/input bindings:
            PatchInputBindingMetadata(function, inputBindingProperties, toolProperties);
        }
    }

    private static void PatchInputBindingMetadata(IFunctionMetadata function, Dictionary<string, ToolPropertyBinding> inputBindingProperties, List<ToolProperty>? toolProperties)
    {
        if (toolProperties is null
            || toolProperties.Count == 0
            || inputBindingProperties.Count == 0)
        {
            return;
        }

        foreach (var property in toolProperties)
        {
            if (inputBindingProperties.TryGetValue(property.Name, out var reference))
            {
                reference.Binding[Constants.McpToolPropertyType] = property.Type;
                function.RawBindings![reference.Index] = reference.Binding.ToJsonString();
            }
        }
    }

    private bool GetToolProperties(string? toolName, IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<ToolProperty>? toolProperties)
    {
        toolProperties = null;

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

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
        var match = GetEntryPointRegex().Match(functionMetadata.EntryPoint ?? string.Empty);

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

        var parameters = method.GetParameters();
        var properties = new List<ToolProperty>(capacity: parameters.Length);

        foreach (var parameter in parameters)
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

        McpToolPropertyType propertyType = parameter.ParameterType.MapToToolPropertyType();

        toolProperty = new(toolAttribute.PropertyName, propertyType.TypeName, toolAttribute.Description,
                           toolAttribute.IsRequired, propertyType.IsArray, propertyType.IsEnum ? propertyType.EnumValues : []);

        return true;
    }

    private static bool TryGetToolPropertiesFromToolTriggerAttribute(ParameterInfo parameter, out List<ToolProperty> toolProperties)
    {
        toolProperties = [];

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

            McpToolPropertyType propertyType = property.PropertyType.MapToToolPropertyType();

            toolProperties.Add(new(property.Name, propertyType.TypeName, property.GetDescription(),
                                   property.IsRequired(), propertyType.IsArray, propertyType.IsEnum ? propertyType.EnumValues : []));
        }

        return toolProperties.Count > 0;
    }

    [GeneratedRegex(@"^(?<typename>.*)\.(?<methodname>\S*)$")]
    private static partial Regex GetEntryPointRegex();

    private record ToolPropertyBinding(int Index, JsonObject Binding);
}
