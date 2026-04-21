// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Generates MCP tool input JSON schemas from worker-side metadata.
/// Output shape mirrors the host's <c>PropertyBasedToolInputSchema</c> so that a
/// schema produced by the worker (with <c>useWorkerInputSchema = true</c>) is
/// byte-equivalent to the schema the host would generate from the same
/// <c>toolProperties</c>.
/// </summary>
internal static class InputSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema from a flat list of tool properties.
    /// Produces <c>{"type":"object","properties":{...},"required":[...]}</c>;
    /// <c>properties</c> and <c>required</c> are always present even when empty.
    /// </summary>
    public static JsonObject GenerateFromToolProperties(IEnumerable<ToolProperty> toolProperties)
    {
        ArgumentNullException.ThrowIfNull(toolProperties);

        var properties = new JsonObject();
        var required = new JsonArray();
        var seenRequired = new HashSet<string>(StringComparer.Ordinal);

        foreach (var p in toolProperties)
        {
            if (p is null || string.IsNullOrWhiteSpace(p.Name))
            {
                continue;
            }

            properties[p.Name] = BuildPropertySchema(p);

            if (p.IsRequired && seenRequired.Add(p.Name))
            {
                required.Add(p.Name);
            }
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
        };
    }

    private static JsonObject BuildPropertySchema(ToolProperty p)
    {
        var node = new JsonObject();

        if (p.IsArray)
        {
            // Order: type=array, items{type, enum?}, description
            node["type"] = "array";

            var items = new JsonObject { ["type"] = p.Type };
            AddEnumIfPresent(items, p.EnumValues);
            node["items"] = items;
        }
        else
        {
            // Order: type, enum?, description
            node["type"] = p.Type;
            AddEnumIfPresent(node, p.EnumValues);
        }

        node["description"] = p.Description ?? string.Empty;
        return node;
    }

    private static void AddEnumIfPresent(JsonObject target, IReadOnlyList<string>? enumValues)
    {
        if (enumValues is null || enumValues.Count == 0)
        {
            return;
        }

        var arr = new JsonArray();
        foreach (var v in enumValues)
        {
            arr.Add(v);
        }

        target["enum"] = arr;
    }
}
