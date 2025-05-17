namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class DefaultMcpInstanceIdProvider : IMcpInstanceIdProvider
{
    public string InstanceId { get; } = Utility.CreateId();
}