using Microsoft.Azure.Functions.Extensions.Mcp;

namespace Microsoft.Extensions.Hosting
{
    internal class DefaultMcpInstanceIdProvider : IMcpInstanceIdProvider
    {
        public DefaultMcpInstanceIdProvider()
        {
            InstanceId = Guid.NewGuid().ToString();
        }

        public string InstanceId { get; }
    }
}