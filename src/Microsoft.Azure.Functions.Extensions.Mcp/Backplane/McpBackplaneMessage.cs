using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane;

public sealed class McpBackplaneMessage
{
    public string ClientId { get; set; } = string.Empty;

    public IJsonRpcMessage Message { get; set; } = default!;
}