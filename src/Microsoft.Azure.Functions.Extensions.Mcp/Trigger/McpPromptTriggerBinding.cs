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
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpPromptTriggerBinding : ITriggerBinding
{
    private readonly IPromptRegistry _promptRegistry;
    private readonly McpPromptTriggerAttribute _promptAttribute;
    private readonly ParameterInfo _triggerParameter;
    private readonly IReadOnlyDictionary<string, object?> _promptMetadata;

    public McpPromptTriggerBinding(
        ParameterInfo triggerParameter,
        IPromptRegistry promptRegistry,
        McpPromptTriggerAttribute promptAttribute,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _promptRegistry = promptRegistry;
        _promptAttribute = promptAttribute;
        _triggerParameter = triggerParameter;

        var logger = loggerFactory.CreateLogger<McpPromptTriggerBinding>();
        _promptMetadata = MetadataParser.ParseMetadata(promptAttribute.Metadata, logger);

        BindingDataContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { triggerParameter.Name!, triggerParameter.ParameterType },
            { "mcppromptcontext", typeof(PromptInvocationContext) },
            { "mcpsessionid", typeof(string) },
            { "mcppromptargs", typeof(IDictionary<string, string>) },
            { "$return", typeof(object).MakeByRefType() }
        };
    }

    public Type TriggerValueType { get; } = typeof(object);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        if (value is not GetPromptExecutionContext executionContext)
        {
            throw new InvalidOperationException(
                $"Cannot get a prompt without a value of type {nameof(GetPromptExecutionContext)}.");
        }

        PromptInvocationContext invocationContext = CreateInvocationContext(executionContext);

        object? triggerValue = invocationContext;
        if (_triggerParameter.ParameterType == typeof(string))
        {
            triggerValue = JsonSerializer.Serialize(invocationContext, McpJsonSerializerOptions.DefaultOptions);
        }

        var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["mcppromptcontext"] = invocationContext,
            [_triggerParameter.Name!] = triggerValue!,
        };

        if (invocationContext.SessionId is not null)
        {
            bindingData["mcpsessionid"] = invocationContext.SessionId;
        }

        if (executionContext.Request.Arguments is { } arguments)
        {
            bindingData["mcppromptargs"] = arguments.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString() ?? string.Empty);
        }

        IValueProvider valueProvider = new ObjectValueProvider(triggerValue, _triggerParameter.ParameterType);
        IValueBinder returnValueBinder = _promptAttribute.UseResultSchema
            ? new PromptReturnValueBinder(executionContext)
            : new SimplePromptReturnValueBinder(executionContext);

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = returnValueBinder
        };

        return Task.FromResult<ITriggerData>(data);
    }

    private PromptInvocationContext CreateInvocationContext(GetPromptExecutionContext executionContext)
    {
        Transport transport = GetTransportInformation(executionContext);

        var invocationContext = new PromptInvocationContext
        {
            Name = executionContext.Request.Name,
            Arguments = executionContext.Request.Arguments,
            SessionId = transport.SessionId ?? executionContext.RequestContext.Server.SessionId,
            ClientInfo = executionContext.RequestContext.Server.ClientInfo,
            Transport = transport
        };

        return invocationContext;
    }

    private Transport GetTransportInformation(GetPromptExecutionContext context) =>
        McpTriggerTransportHelper.GetTransportInformation(context.RequestContext.Services);

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var arguments = ParsePromptArguments(_promptAttribute);
        var icons = ParseIcons(_promptAttribute);

        var listener = new McpPromptListener(
            context.Executor,
            context.Descriptor.ShortName,
            _promptAttribute.PromptName,
            _promptAttribute.Title,
            _promptAttribute.Description,
            arguments,
            icons,
            _promptMetadata);

        _promptRegistry.Register(listener);

        return Task.FromResult<IListener>(listener);
    }

    internal static IReadOnlyList<PromptArgument>? ParsePromptArguments(McpPromptTriggerAttribute attribute)
    {
        if (string.IsNullOrEmpty(attribute.PromptArguments))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<PromptArgument>>(
            attribute.PromptArguments,
            McpJsonSerializerOptions.DefaultOptions);
    }

    internal static IList<Icon>? ParseIcons(McpPromptTriggerAttribute attribute)
    {
        if (string.IsNullOrEmpty(attribute.Icons))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<Icon>>(
            attribute.Icons,
            McpJsonSerializerOptions.DefaultOptions);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = "McpPromptTrigger",
            Type = "McpPromptTrigger",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "MCP prompt trigger",
                Prompt = "Enter MCP prompt trigger value"
            }
        };
    }
}
