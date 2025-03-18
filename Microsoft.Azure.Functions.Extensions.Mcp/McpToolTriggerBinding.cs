using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolTriggerBinding(IToolRegistry toolRegistry) : ITriggerBinding
{
    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
    {
        { "data", typeof(object) },
        { "$return", typeof(object).MakeByRefType() }
    };

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var bindingData = new Dictionary<string, object>();

        var data = new TriggerData(bindingData)
        {
            ReturnValueProvider = new McpToolTriggerReturnValueBinder()
        };

        return Task.FromResult<ITriggerData>(data);
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new McpToolListener(context.Executor, "foo", "fooTool");

        toolRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = "McpToolTrigger",
            Type = "McpToolTrigger",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "MCP tool trigger",
                Prompt = "Enter MCP tool trigger value"
            }
        };
    }

    internal class McpToolTriggerReturnValueBinder : IValueBinder
    {
        public Type Type { get; } = typeof(object);


        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {

            // Set return.
            return Task.CompletedTask;
        }

        public Task<object> GetValueAsync()
        {
            throw new NotSupportedException();
        }

        public string ToInvokeString()
        {
            return string.Empty;
        }

    }
}