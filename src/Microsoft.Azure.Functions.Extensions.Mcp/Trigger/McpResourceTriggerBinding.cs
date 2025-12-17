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

internal sealed class McpResourceTriggerBinding : ITriggerBinding
{
    private readonly IResourceRegistry _resourceRegistry;
    private readonly McpResourceTriggerAttribute _resourceAttribute;
    private readonly ParameterInfo _triggerParameter;
    private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _resourceMetadata;

    public McpResourceTriggerBinding(ParameterInfo triggerParameter, IResourceRegistry resourceRegistry, McpResourceTriggerAttribute resourceAttribute)
    {
        ArgumentNullException.ThrowIfNull(triggerParameter);

        _resourceRegistry = resourceRegistry;
        _resourceAttribute = resourceAttribute;
        _triggerParameter = triggerParameter;
        _resourceMetadata = GetMetadata(resourceAttribute, triggerParameter);

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
        IValueBinder returnValueBinder = new ResourceReturnValueBinder(executionContext, _resourceAttribute, _resourceMetadata);

        var data = new TriggerData(valueProvider, bindingData)
        {
            ReturnValueProvider = returnValueBinder
        };

        return Task.FromResult<ITriggerData>(data);
    }

    private ResourceInvocationContext CreateInvocationContext(ReadResourceExecutionContext executionContext)
    {
        Transport transport = GetTransportInformation(executionContext);

        var invocationContext = new ResourceInvocationContext
        {
            Uri = executionContext.Request.Uri,
            SessionId = transport.SessionId ?? executionContext.RequestContext.Server.SessionId,
            ClientInfo = executionContext.RequestContext.Server.ClientInfo,
            Transport = transport
        };

        return invocationContext;
    }

    private Transport GetTransportInformation(ReadResourceExecutionContext context)
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
        var listener = new McpResourceListener(
            context.Executor,
            context.Descriptor.ShortName,
            _resourceAttribute.Uri,
            _resourceAttribute.ResourceName,
            _resourceAttribute.Description,
            _resourceAttribute.MimeType,
            _resourceAttribute.Size,
            _resourceMetadata);
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
        List<KeyValuePair<string, object?>>? metadata = null;

        // First check if metadata was serialized from worker model
        if (attribute.Metadata is { } metadataString)
        {
            var metadataList = JsonSerializer.Deserialize<List<KeyValuePair<string, object?>>>(metadataString, McpJsonSerializerOptions.DefaultOptions);
            if (metadataList is not null)
            {
                metadata = metadataList;
            }
        }
        else
        {
            // Fallback to reading from attributes
            var metadataAttributes = parameter.GetCustomAttributes<McpResourceMetadataAttribute>();
            
            if (metadataAttributes.Any())
            {
                metadata = [];
                foreach (var attr in metadataAttributes)
                {
                    // Try to parse string values as JSON
                    object? value = attr.Value;
                    if (attr.Value is string stringValue && TryParseJson(stringValue, out var jsonValue))
                    {
                        value = jsonValue;
                    }
                    metadata.Add(new KeyValuePair<string, object?>(attr.Key, value));
                }
            }
        }

        return metadata ?? [];
    }

    private static bool TryParseJson(string value, out object? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<JsonElement>(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}