// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Extracts tool properties from function parameters and attributes.
/// </summary>
internal static class ToolPropertyParser
{
    /// <summary>
    /// Serializes tool properties to a JSON node.
    /// </summary>
    public static JsonNode? GetPropertiesJson(List<ToolProperty> properties)
    {
        return JsonSerializer.Serialize(properties);
    }
}
