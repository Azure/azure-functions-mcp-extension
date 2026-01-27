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
internal static class MetadataExtractor
{
    /// <summary>
    /// Gets resource metadata from function metadata.
    /// </summary>
    public static bool TryGetResourceMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
    {
        return TryGetMetadata<McpResourceTriggerAttribute, McpMetadataAttribute>(functionMetadata, out metadata);
    }

    /// <summary>
    /// Generic method to extract metadata from function parameters based on trigger and metadata attribute types.
    /// </summary>
    public static bool TryGetMetadata<TTriggerAttribute, TMetadataAttribute>(
        IFunctionMetadata functionMetadata,
        [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
        where TTriggerAttribute : Attribute
        where TMetadataAttribute : Attribute, IKeyValueMetadataAttribute
    {
        metadata = null;

        if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method))
        {
            return false;
        }

        return TryExtractMetadataFromParameter<TTriggerAttribute, TMetadataAttribute>(
            method!.GetParameters(),
            out metadata);
    }

    /// <summary>
    /// Extracts metadata attributes from parameters that have the specified trigger attribute.
    /// </summary>
    public static bool TryExtractMetadataFromParameter<TTriggerAttribute, TMetadataAttribute>(
        ParameterInfo[] parameters,
        [NotNullWhen(true)] out List<KeyValuePair<string, object?>>? metadata)
        where TTriggerAttribute : Attribute
        where TMetadataAttribute : Attribute, IKeyValueMetadataAttribute
    {
        metadata = null;

        foreach (var parameter in parameters)
        {
            var triggerAttribute = parameter.GetCustomAttribute<TTriggerAttribute>();
            if (triggerAttribute is null)
            {
                continue;
            }

            var metadataAttributes = parameter.GetCustomAttributes<TMetadataAttribute>();
            if (!metadataAttributes.Any())
            {
                continue;
            }

            metadata = [];
            foreach (var attr in metadataAttributes)
            {
                if (attr.Key is { Length: > 0 })
                {
                    metadata.Add(new KeyValuePair<string, object?>(attr.Key, attr.Value));
                }
            }

            return true;
        }

        return false;
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
