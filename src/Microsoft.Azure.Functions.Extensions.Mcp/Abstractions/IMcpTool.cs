// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    public ICollection<IMcpToolProperty> Properties { get; set; }

    Task<CallToolResponse> RunAsync(CallToolRequestParams callToolRequest, CancellationToken cancellationToken);
}

internal static class McpToolExtensions
{
    internal static JsonElement GetPropertiesInputSchema(this IMcpTool tool)
    {
        var schema = new
        {
            type = "object",
            properties = tool.Properties.ToDictionary(
                prop => prop.PropertyName,
                prop => new
                {
                    type = prop.PropertyType,
                    description = prop.Description ?? string.Empty
                }
            ),
            required = tool.Properties.Where(prop => prop.Required)
                .Select(prop => prop.PropertyName).ToArray()
        };

        var jsonString = JsonSerializer.Serialize(schema);
        using var document = JsonDocument.Parse(jsonString);
        return document.RootElement.Clone();
    }
}
