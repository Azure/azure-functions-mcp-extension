// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using ModelContextProtocol.Protocol.Types;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class ToolPropertyInputBinding(McpToolPropertyAttribute attribute) : IBinding
{
    public Task<IValueProvider> BindAsync(BindingContext context)
    {
        // Access the trigger's binding data
        var bindingData = context.BindingData;

        if (bindingData["mcptoolcontext"] is not CallToolRequestParams toolContext)
        {
            throw new InvalidOperationException("Tool context is not available.");
        }

        if (toolContext.Arguments == null
            || !toolContext.Arguments.TryGetValue(attribute.PropertyName, out var propertyValue))
        {
            propertyValue = default;
        }

        IValueProvider valueProvider = new ToolPropertyValueProvider(propertyValue.ToString());
        return Task.FromResult(valueProvider);
    }

    public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
    {
        throw new NotImplementedException();
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = "ToolProperty",
            Type = "ToolProperty",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "Tool property binding",
                Prompt = "Tool property binding"
            }
        };
    }

    public bool FromAttribute => true;
}