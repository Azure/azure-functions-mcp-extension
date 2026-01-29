// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;

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
            Tools = _tools.Values.Select(static tool => new Tool
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.GetPropertiesInputSchema(),
                Meta = MetadataParser.SerializeMetadata(tool.Metadata)
            }).ToList()
        };

        return new ValueTask<ListToolsResult>(result);
    }
}
