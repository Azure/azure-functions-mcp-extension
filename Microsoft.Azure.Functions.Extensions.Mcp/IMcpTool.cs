using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    Task<object> RunAsync(CancellationToken cancellationToken);
}