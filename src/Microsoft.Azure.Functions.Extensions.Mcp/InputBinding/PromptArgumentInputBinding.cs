// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class PromptArgumentInputBinding(McpPromptArgumentAttribute attribute) : IBinding
{
    public Task<IValueProvider> BindAsync(BindingContext context)
    {
        var bindingData = context.BindingData;

        if (bindingData["mcppromptcontext"] is not PromptInvocationContext promptContext)
        {
            throw new InvalidOperationException("Prompt context is not available.");
        }

        string? value = null;

        if (promptContext.Arguments != null
            && promptContext.Arguments.TryGetValue(attribute.ArgumentName, out var argValue))
        {
            value = argValue.ToString();
        }

        IValueProvider valueProvider = new PromptArgumentValueProvider(value);
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
            Name = "PromptArgument",
            Type = "PromptArgument",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "Prompt argument binding",
                Prompt = "Prompt argument binding"
            }
        };
    }

    public bool FromAttribute => true;
}
