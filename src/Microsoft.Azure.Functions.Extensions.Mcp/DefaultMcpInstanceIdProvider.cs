using Microsoft.Azure.Functions.Extensions.Mcp;

namespace Microsoft.Extensions.Hosting
{
    internal class DefaultMcpInstanceIdProvider : IMcpInstanceIdProvider
    {
        public string InstanceId { get; } = Guid.NewGuid().ToString();
    }
}