// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Extracts metadata from function parameters.
/// </summary>
internal static class MetadataParser
{
    /// <summary>
    /// Gets resource metadata JSON from function metadata.
    /// </summary>
    public static bool TryGetResourceMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out string? metadataJson)
    {
        return TryGetMetadata<McpResourceTriggerAttribute>(functionMetadata, out metadataJson);
    }

    /// <summary>
    /// Gets tool metadata JSON from function metadata.
    /// </summary>
    public static bool TryGetToolMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out string? metadataJson)
    {
        return TryGetMetadata<McpToolTriggerAttribute>(functionMetadata, out metadataJson);
    }

    /// <summary>
    /// Generic method to extract metadata JSON from function parameters based on trigger attribute type.
    /// </summary>
    public static bool TryGetMetadata<TTriggerAttribute>(
        IFunctionMetadata functionMetadata,
        [NotNullWhen(true)] out string? metadataJson)
        where TTriggerAttribute : Attribute
    {
        metadataJson = null;

        if (!FunctionMethodResolver.TryResolveMethod(functionMetadata, out var method))
        {
            return false;
        }

        return TryExtractMetadataFromParameter<TTriggerAttribute>(
            method!.GetParameters(),
            out metadataJson);
    }

    /// <summary>
    /// Extracts MCP metadata attribute JSON from the parameter that has the specified trigger attribute.
    /// </summary>
    public static bool TryExtractMetadataFromParameter<TTriggerAttribute>(
        ParameterInfo[] parameters,
        [NotNullWhen(true)] out string? metadataJson)
        where TTriggerAttribute : Attribute
    {
        metadataJson = null;

        var triggerParameter = Array.Find(
            parameters,
            parameter => Attribute.IsDefined(parameter, typeof(TTriggerAttribute), inherit: false));

        if (triggerParameter is null)
        {
            return false;
        }

        var metadataAttr = triggerParameter.GetCustomAttribute<McpMetadataAttribute>(inherit: false);
        if (metadataAttr is null || string.IsNullOrWhiteSpace(metadataAttr.Json))
        {
            return false;
        }

        // Validate it's valid JSON
        try
        {
            using var doc = JsonDocument.Parse(metadataAttr.Json);
            metadataJson = metadataAttr.Json;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
