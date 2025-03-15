using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public sealed class McpTriggerBindingProvider(IMcpRequestHandler requestHandler) : ITriggerBindingProvider
{
    public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
    {
        var binding = new McpTriggerBinding(requestHandler);

        return Task.FromResult<ITriggerBinding>(binding);
    }
}