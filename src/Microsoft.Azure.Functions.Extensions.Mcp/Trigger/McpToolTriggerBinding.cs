// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
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

    private Transport GetTransportInformation(CallToolExecutionContext context) =>
        McpTriggerTransportHelper.GetTransportInformation(context.RequestContext.Services);

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        ToolInputSchema inputSchema = CreateToolInputSchema();
        
        var listener = new McpToolListener(context.Executor, context.Descriptor.ShortName,
            _toolAttribute.ToolName, _toolAttribute.Description, inputSchema);

        _toolRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }

    /// <summary>
    /// Creates the appropriate tool input schema handler based on the tool attribute configuration.
    /// </summary>
    /// <returns>A ToolInputSchema instance.</returns>
    private ToolInputSchema CreateToolInputSchema()
    {
        if (_toolAttribute.UseWorkerInputSchema)
        {
            var inputSchema = GetInputSchema(_toolAttribute);
            if (inputSchema is null)
            {
                throw new InvalidOperationException(
                   $"InputSchema is null or invalid. ");
            }
            return new JsonSchemaToolInputSchema(inputSchema);
        }
        else
        {
            var toolProperties = GetProperties(_toolAttribute, _triggerParameter);
            return new PropertyBasedToolInputSchema(toolProperties);
        }
    }

    internal static JsonDocument? GetInputSchema(McpToolTriggerAttribute attribute)
    {
        if (string.IsNullOrEmpty(attribute.InputSchema))
        {
            return null;
        }

        var doc = JsonDocument.Parse(attribute.InputSchema);
        try
        {
            // Validate that the parsed schema is a valid MCP tool input schema
            if (!McpInputSchemaJsonUtilities.IsValidMcpToolSchema(doc))
            {
                doc.Dispose();
                throw new ArgumentException(
                    "The specified document is not a valid MCP tool input JSON schema.",
                    nameof(attribute.InputSchema));
            }

            return doc;
        }
        catch (JsonException ex)
        {
            doc?.Dispose();
            throw new InvalidOperationException(
                $"Failed to parse InputSchema for tool '{attribute.ToolName}'. Schema must be valid JSON.", ex);
        }
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
