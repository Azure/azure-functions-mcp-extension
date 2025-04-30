using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane;

public sealed class McpBackplaneMessage
{
    public string ClientId { get; set; } = string.Empty;

    public JsonRpcMessage Message { get; set; } = default!;
}