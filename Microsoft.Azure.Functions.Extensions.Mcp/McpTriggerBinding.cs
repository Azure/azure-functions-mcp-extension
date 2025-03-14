using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public sealed class McpTriggerBinding : ITriggerBinding
{
    public Type TriggerValueType { get; }

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        throw new NotImplementedException();
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new McpListener(context.Executor);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = "McpTrigger",
            Type = "McpTrigger",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "MCP Trigger",
                Prompt = "Enter MCP trigger value"
            }
        };
    }

   
}