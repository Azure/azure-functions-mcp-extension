// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Serialization;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Extracts metadata from function parameters and builds JSON representations.
/// </summary>
internal static class MetadataParser
{
    /// <summary>
    /// Gets resource metadata from function metadata.
    /// </summary>
    public static bool TryGetResourceMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
    {
        return TryGetMetadata<McpResourceTriggerAttribute>(functionMetadata, out metadata);
    }

    /// <summary>
    /// Generic method to extract metadata from function parameters based on trigger attribute type.
    /// </summary>
    public static bool TryGetMetadata<TTriggerAttribute>(
        IFunctionMetadata functionMetadata,
        [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
        where TTriggerAttribute : Attribute
    {
        metadata = null;

        if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method))
        {
            return false;
        }

        return TryExtractMetadataFromParameter<TTriggerAttribute>(
            method!.GetParameters(),
            out metadata);
    }

    /// <summary>
    /// Extracts MCP metadata attributes from parameters that have the specified trigger attribute.
    /// </summary>
    public static bool TryExtractMetadataFromParameter<TTriggerAttribute>(
        ParameterInfo[] parameters,
        [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
        where TTriggerAttribute : Attribute
    {
        metadata = null;

        var triggerParameter = Array.Find(
            parameters,
            parameter => Attribute.IsDefined(parameter, typeof(TTriggerAttribute), inherit: false));

        if (triggerParameter is null
            || !Attribute.IsDefined(triggerParameter, typeof(McpMetadataAttribute), inherit: false))
        {
            return false;
        }

        metadata = [];
        foreach (var attr in triggerParameter.GetCustomAttributes<McpMetadataAttribute>(inherit: false))
        {
            if (!string.IsNullOrEmpty(attr.Key))
            {
                metadata.Add(new KeyValuePair<string, object?>(attr.Key, attr.Value));
            }
        }

        if (metadata.Count == 0)
        {
            metadata = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a list of key-value pairs to a nested JSON string.
    /// Supports colon notation for nested paths (e.g., "ui:resourceUri" becomes { "ui": { "resourceUri": "..." } }).
    /// </summary>
    public static string BuildMetadataJson(List<KeyValuePair<string, object?>> metadata)
    {
        return JsonSerializer.Serialize(metadata, MetadataSerializerOptions);
    }

    private static JsonSerializerOptions MetadataSerializerOptions { get; } = CreateMetadataSerializerOptions();

    private static JsonSerializerOptions CreateMetadataSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MetadataListJsonConverter());
        return options;
    }
}
