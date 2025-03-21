using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System.Reflection;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpTriggerBindingProvider(IToolRegistry toolRegistry) : ITriggerBindingProvider
{
    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parameterInfo = context.Parameter;
        var attribute = parameterInfo.GetCustomAttribute<McpToolTriggerAttribute>(false);
        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        var binding = new McpToolTriggerBinding(context.Parameter, toolRegistry, attribute.Name, attribute.Description);

        return Task.FromResult<ITriggerBinding?>(binding);
    }
}