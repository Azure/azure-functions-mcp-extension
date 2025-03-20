using System.Reflection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolTriggerBinding : ITriggerBinding
{
    private readonly IToolRegistry _toolRegistry;
    private readonly string _toolName;
    private readonly string? _toolDescription;

    public McpToolTriggerBinding(ParameterInfo triggerParameter, IToolRegistry toolRegistry, string toolName, string? toolDescription)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _toolRegistry = toolRegistry;
        _toolName = toolName;
        _toolDescription = toolDescription;

        BindingDataContract = new Dictionary<string, Type>
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "$return", typeof(object).MakeByRefType() }
        };
    }

    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var bindingData = new Dictionary<string, object>();
        var valueProvider = new ObjectValueProvider(value, typeof(object));

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = new McpToolTriggerReturnValueBinder(),
        };

        return Task.FromResult<ITriggerData>(data);
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new McpToolListener(context.Executor, context.Descriptor.ShortName, _toolName, _toolDescription);

        _toolRegistry.Register(listener);

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

internal class ObjectValueProvider : IValueProvider
{
    private readonly object? value;
    private readonly Task<object?> valueAsTask;

    public ObjectValueProvider(object value, Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (value != null && !valueType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Cannot convert {value} to {valueType.Name}.");
        }

        this.value = value;
        this.valueAsTask = Task.FromResult(value);
        this.Type = valueType;
    }

    public Type Type { get; }

    public Task<object?> GetValueAsync()
    {
        return this.valueAsTask;
    }

    public string? ToInvokeString()
    {
        return value?.ToString();
    }
}