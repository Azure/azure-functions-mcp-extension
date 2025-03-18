using Microsoft.Azure.WebJobs.Host.Triggers;
using System.Reflection;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpTriggerBindingProvider(IToolRegistry toolRegistry) : ITriggerBindingProvider
{
    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        var parameterInfo = context.Parameter;
        var attribute = parameterInfo.GetCustomAttribute<McpToolTriggerAttribute>(false);
        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }


        var binding = new McpToolTriggerBinding(toolRegistry);

        return Task.FromResult<ITriggerBinding?>(binding);
    }
}