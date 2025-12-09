// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

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

        BindingDataContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "mcptoolcontext", typeof(ToolInvocationContext) },
            { "mcpsessionid", typeof(string) },
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

        ToolInvocationContext invocationContext = CreateInvocationContext(executionContext);

        object? triggerValue = invocationContext;
        if (_triggerParameter.ParameterType == typeof(string))
        {
            triggerValue = JsonSerializer.Serialize(invocationContext, McpJsonSerializerOptions.DefaultOptions);
        }

        var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["mcptoolcontext"] = invocationContext,
            [_triggerParameter.Name!] = triggerValue,
        };

        if (invocationContext.SessionId is not null)
        {
            bindingData["mcpsessionid"] = invocationContext.SessionId;
        }

        if (executionContext.Request.Arguments is { } arguments)
        {
            bindingData["mcptoolargs"] = arguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty);
        }

        IValueProvider valueProvider = new ObjectValueProvider(triggerValue, _triggerParameter.ParameterType);
        IValueBinder returnValueBinder = _toolAttribute.UseResultSchema
            ? new ToolReturnValueBinder(executionContext)
            : new SimpleToolReturnValueBinder(executionContext);

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = returnValueBinder
        };

        return Task.FromResult<ITriggerData>(data);
    }

    private ToolInvocationContext CreateInvocationContext(CallToolExecutionContext executionContext)
    {
        Transport transport = GetTransportInformation(executionContext);

        var invocationContext = new ToolInvocationContext
        {
            Name = executionContext.Request.Name,
            Arguments = executionContext.Request.Arguments,
            SessionId = transport.SessionId ?? executionContext.RequestContext.Server.SessionId,
            ClientInfo = executionContext.RequestContext.Server.ClientInfo,
            Transport = transport
        };

        return invocationContext;
    }

    private Transport GetTransportInformation(CallToolExecutionContext context)
    {
        if (context.RequestContext.Services?.GetService(typeof(IHttpContextAccessor)) is IHttpContextAccessor contextAccessor
            && contextAccessor.HttpContext is not null)
        {
            var name = contextAccessor.HttpContext.Items[McpConstants.McpTransportName] as string ?? "http";

            var transport = new Transport
            {
                Name = name
            };

            var headers = contextAccessor.HttpContext.Request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value!), StringComparer.OrdinalIgnoreCase);
            transport.Properties.Add("headers", headers);

            if (headers.TryGetValue(McpConstants.McpSessionIdHeaderName, out var sessionId))
            {
                transport.SessionId = sessionId;
            }

            return transport;
        }

        return new Transport
        {
            Name = "unknown"
        };
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        IList<IMcpToolProperty> toolProperties = [];

        // Generate tool properties only if input schema was not generated in the worker
        if (!_toolAttribute.UseWorkerInputSchema)
        {
            toolProperties = GetProperties(_toolAttribute, _triggerParameter);
        }

        var listener = new McpToolListener(context.Executor, context.Descriptor.ShortName, 
            _toolAttribute.ToolName, _toolAttribute.Description, toolProperties, _toolAttribute.InputSchema ?? McpInputSchemaJsonUtilities.DefaultMcpToolSchema);

        _toolRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }

    private static List<IMcpToolProperty> GetProperties(McpToolTriggerAttribute attribute, ParameterInfo triggerParameter)
    {
        List<IMcpToolProperty>? toolProperties = null;

        if (attribute.ToolProperties is { } propertiesString)
        {
            var arguments = JsonSerializer.Deserialize<List<McpToolPropertyAttribute>>(propertiesString, McpJsonSerializerOptions.DefaultOptions);
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

            // Set the tool properties string from the attributes found on the method parameters.
            attribute.ToolProperties = JsonSerializer.Serialize(toolProperties, McpJsonSerializerOptions.DefaultOptions);
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
}
