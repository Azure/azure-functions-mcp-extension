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
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpResourceTriggerBinding : ITriggerBinding
{
    private readonly IResourceRegistry _resourceRegistry;
    private readonly McpResourceTriggerAttribute _resourceAttribute;
    private readonly ParameterInfo _triggerParameter;
    private readonly ILoggerFactory _loggerFactory;

    public McpResourceTriggerBinding(
        ParameterInfo triggerParameter,
        IResourceRegistry resourceRegistry,
        McpResourceTriggerAttribute resourceAttribute,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _resourceRegistry = resourceRegistry;
        _resourceAttribute = resourceAttribute;
        _triggerParameter = triggerParameter;
        _loggerFactory = loggerFactory;

        BindingDataContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "mcpresourceuri", typeof(string) },
            { "mcpresourcecontext", typeof(ResourceInvocationContext) },
            { "mcpsessionid", typeof(string) },
            { "$return", typeof(object).MakeByRefType() }
        };
    }

    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        if (value is not ReadResourceExecutionContext executionContext)
        {
            throw new InvalidOperationException($"Cannot read a resource without a value of type {nameof(ReadResourceExecutionContext)}.");
        }

        ResourceInvocationContext invocationContext = CreateInvocationContext(executionContext);

        object? triggerValue = invocationContext;
        if (_triggerParameter.ParameterType == typeof(string))
        {
            triggerValue = JsonSerializer.Serialize(invocationContext, McpJsonSerializerOptions.DefaultOptions);
        }

        var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["mcpresourcecontext"] = invocationContext,
            [_triggerParameter.Name!] = triggerValue!,
        };

        if (invocationContext.SessionId is not null)
        {
            bindingData["mcpsessionid"] = invocationContext.SessionId;
        }

        if (executionContext.Request.Uri is not null)
        {
            bindingData["mcpresourceuri"] = executionContext.Request.Uri;
        }

        IValueProvider valueProvider = new ObjectValueProvider(triggerValue, _triggerParameter.ParameterType);
        IValueBinder returnValueBinder = new ResourceReturnValueBinder(
            executionContext,
            _resourceAttribute,
            _loggerFactory.CreateLogger<ResourceReturnValueBinder>());

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = returnValueBinder
        };

        return Task.FromResult<ITriggerData>(data);
    }

    private ResourceInvocationContext CreateInvocationContext(ReadResourceExecutionContext executionContext)
    {
        Transport transport = GetTransportInformation(executionContext);

        var invocationContext = new ResourceInvocationContext(executionContext.Request.Uri)
        {
            SessionId = transport.SessionId ?? executionContext.RequestContext.Server.SessionId,
            ClientInfo = executionContext.RequestContext.Server.ClientInfo,
            Transport = transport
        };

        return invocationContext;
    }

    private Transport GetTransportInformation(ReadResourceExecutionContext context) =>
        McpTriggerTransportHelper.GetTransportInformation(context.RequestContext.Services);

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var resourceMetadata = GetMetadata(_resourceAttribute, _triggerParameter);

        var listener = new McpResourceListener(
            context.Executor,
            context.Descriptor.ShortName,
            _resourceAttribute.Uri,
            _resourceAttribute.ResourceName,
            _resourceAttribute.Description,
            _resourceAttribute.MimeType,
            _resourceAttribute.Size,
            resourceMetadata);

        _resourceRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = "McpResourceTrigger",
            Type = "McpResourceTrigger",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "MCP resource trigger",
                Prompt = "Enter MCP resource trigger value"
            }
        };
    }

    private static IReadOnlyCollection<KeyValuePair<string, object?>> GetMetadata(McpResourceTriggerAttribute attribute, ParameterInfo parameter)
    {
        return [];
    }
}
