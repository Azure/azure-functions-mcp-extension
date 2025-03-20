using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    Task<CallToolResponse> RunAsync(CallToolRequestParams callToolRequest, CancellationToken cancellationToken);
}