// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using ModelContextProtocol.Protocol.Types;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolTriggerBinding : ITriggerBinding
{
    private readonly IToolRegistry _toolRegistry;
    private readonly McpToolTriggerAttribute _toolAttribute;
    private readonly ParameterInfo _triggerParameter;

    public McpToolTriggerBinding(ParameterInfo triggerParameter, IToolRegistry toolRegistry, McpToolTriggerAttribute toolAttribute)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _toolRegistry = toolRegistry;
        _toolAttribute = toolAttribute;
        _triggerParameter = triggerParameter;

        BindingDataContract = new Dictionary<string, Type>
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "mcptoolcontext", typeof(CallToolRequestParams) },
            { "mcptoolargs", typeof(IDictionary<string, string>)},
            { "$return", typeof(object).MakeByRefType() }
        };
    }

    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        if (value is not CallToolExecutionContext executionContext)
        {
            throw new InvalidOperationException($"Cannot execute a tool without a value of type {nameof(CallToolExecutionContext)}.");
        }

        object? triggerValue = executionContext.Request;
        if (_triggerParameter.ParameterType == typeof(string))
        {
            triggerValue = JsonSerializer.Serialize(executionContext.Request, McpJsonSerializerOptions.DefaultOptions);
        }

        var bindingData = new Dictionary<string, object>
        {
            ["mcptoolcontext"] = executionContext.Request,
            [_triggerParameter.Name!] = triggerValue,
        };

        if (executionContext.Request.Arguments is { } arguments)
        {
            bindingData["mcptoolargs"] = arguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty);
        }

        var valueProvider = new ObjectValueProvider(triggerValue, _triggerParameter.ParameterType);

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = new McpToolTriggerReturnValueBinder(executionContext),
        };

        return Task.FromResult<ITriggerData>(data);
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var toolProperties = GetProperties(_toolAttribute, _triggerParameter);

        var listener = new McpToolListener(context.Executor, context.Descriptor.ShortName, _toolAttribute.ToolName, _toolAttribute.Description, toolProperties);

        _toolRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }


    private static List<IMcpToolProperty> GetProperties(McpToolTriggerAttribute attribute, ParameterInfo triggerParameter)
    {
        List<IMcpToolProperty>? toolProperties = null;

        if (attribute.ToolProperties is { } propertiesString)
        {
            var arguments =
                JsonSerializer.Deserialize<List<McpToolPropertyAttribute>>(propertiesString,
                    McpJsonSerializerOptions.DefaultOptions);
            SetProperties(arguments);
        }
        else
        {
            if (triggerParameter.Member is not MethodInfo methodInfo)
            {
                return toolProperties ?? [];
            }

            toolProperties = [];

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

        return toolProperties ?? [];

        void SetProperties(List<McpToolPropertyAttribute>? properties)
        {
            if (properties is not null)
            {
                toolProperties = properties.Cast<IMcpToolProperty>().ToList();
            }
        }
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