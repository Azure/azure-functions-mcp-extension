using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpTriggerBindingProvider(IToolRegistry toolRegistry) : ITriggerBindingProvider
{
    public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
    {
        var binding = new McpToolTriggerBinding(toolRegistry);

        return Task.FromResult<ITriggerBinding>(binding);
    }
}