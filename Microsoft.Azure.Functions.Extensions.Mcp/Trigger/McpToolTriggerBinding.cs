using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System.Reflection;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolTriggerBinding : ITriggerBinding
{
    private readonly IToolRegistry _toolRegistry;
    private readonly string _toolName;
    private readonly string? _toolDescription;
    private readonly ParameterInfo _triggerParameter;

    public McpToolTriggerBinding(ParameterInfo triggerParameter, IToolRegistry toolRegistry, string toolName, string? toolDescription)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _toolRegistry = toolRegistry;
        _toolName = toolName;
        _toolDescription = toolDescription;
        _triggerParameter = triggerParameter;

        BindingDataContract = new Dictionary<string, Type>
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "mcptoolcontext", typeof(ToolInvocationContext) },
            { "$return", typeof(object).MakeByRefType() }
        };
    }

    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        if (value is not CallToolExecutionContext executionContext)
        {

            throw new InvalidOperationException(
                $"Cannot execute a tool without a value of type {nameof(CallToolExecutionContext)}.");
        }

        var bindingData = new Dictionary<string, object>();
        bindingData["mcptoolcontext"] = executionContext.Request;

        var valueProvider = new ObjectValueProvider(executionContext.Request, typeof(object));

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = new McpToolTriggerReturnValueBinder(executionContext),
        };

        return Task.FromResult<ITriggerData>(data);
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var toolProperties = new List<IMcpToolProperty>();

        if (_triggerParameter.Member is MethodInfo methodInfo)
        {
            foreach (var parameter in methodInfo.GetParameters())
            {
                var property = parameter.GetCustomAttribute<McpToolPropertyAttribute>(inherit: false);
                if (property is null)
                {
                    continue;
                }

                toolProperties.Add(property);
            }
        }

        var listener = new McpToolListener(context.Executor, context.Descriptor.ShortName, _toolName, _toolDescription, toolProperties);

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

    internal class McpToolTriggerReturnValueBinder(CallToolExecutionContext executionContext) : IValueBinder
    {
        public Type Type { get; } = typeof(object);


        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            executionContext.SetResult(value);

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
    private readonly object? _value;
    private readonly Task<object?> _valueAsTask;

    public ObjectValueProvider(object? value, Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (value != null && !valueType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Cannot convert {value} to {valueType.Name}.");
        }

        _value = value;
        _valueAsTask = Task.FromResult(value);
        Type = valueType;
    }

    public Type Type { get; }

    public Task<object?> GetValueAsync() => _valueAsTask;

    public string? ToInvokeString() => _value?.ToString();
}