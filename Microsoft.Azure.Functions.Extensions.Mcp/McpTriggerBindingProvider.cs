using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public sealed class McpTriggerBindingProvider : ITriggerBindingProvider
{
    public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
    {
        var binding = new McpTriggerBinding();

        return Task.FromResult<ITriggerBinding>(binding);
    }
}