// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IMcpTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IMcpTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (!_tools.TryAdd(tool.Name, tool))
        {
            throw new InvalidOperationException($"Tool with name '{tool.Name}' is already registered.");
        }
    }

    public bool TryGetTool(string name, [NotNullWhen(true)] out IMcpTool? tool)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _tools.TryGetValue(name, out tool);
    }

    public ICollection<IMcpTool> GetTools()
    {
        return _tools.Values;
    }

    public ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ListToolsResult
        {
            Tools = _tools.Values.Select(static tool =>
            {
                var toolItem = new Tool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = tool.GetPropertiesInputSchema()
                };

                // Hardcode metadata for GetWelcomeMessage tool for testing
                if (tool.Name == "GetWelcomeMessage")
                {
                    toolItem.Meta = new JsonObject
                    {
                        ["openai/outputTemplate"] = "ui://widget/welcome.html"
                    };
                }

                if (tool.Name == "GetFunctionsLogo")
                {
                    toolItem.Meta = new JsonObject
                    {
                        ["openai/outputTemplate"] = "file:///resources/logo.png"
                    };
                }

                return toolItem;
            }).ToList()
        };

        return new ValueTask<ListToolsResult>(result);
    }
}
