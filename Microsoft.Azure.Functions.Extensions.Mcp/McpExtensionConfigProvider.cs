using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Reflection;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[Extension("Mcp")]
internal sealed class McpExtensionConfigProvider(IToolRegistry toolRegistry) : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        context.AddBindingRule<McpToolTriggerAttribute>()
            .BindToTrigger(new McpTriggerBindingProvider(toolRegistry));

        context.AddBindingRule<McpToolPropertyAttribute>()
            .Bind(new McpToolPropertyBindingProvider());
    }
}


public class McpToolPropertyBindingProvider : IBindingProvider
{
    public Task<IBinding?> TryCreateAsync(BindingProviderContext context)
    {
        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<McpToolPropertyAttribute>(inherit: false);
        if (attribute == null)
        {
            return Task.FromResult<IBinding?>(null);
        }

        return Task.FromResult<IBinding?>(new ToolPropertyInputBinding(attribute));
    }
}

public class ToolPropertyInputBinding(McpToolPropertyAttribute attribute) : IBinding
{
    public Task<IValueProvider> BindAsync(BindingContext context)
    {
        // Access the trigger's binding data
        var bindingData = context.BindingData;
        var toolContext = bindingData["mcptoolcontext"] as ToolInvocationContext;

        if (toolContext == null)
        {
            throw new InvalidOperationException("Tool context is not available.");
        }

        if (toolContext.Arguments == null
            || !toolContext.Arguments.TryGetValue(attribute.Name, out var propertyValue))
        {
            propertyValue = null;
        }

        IValueProvider valueProvider = new ToolPropertyValueProvider(propertyValue?.ToString());
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

public class ToolPropertyValueProvider(string? value) : IValueProvider
{
    public Type Type => typeof(string);

    public Task<object?> GetValueAsync() => Task.FromResult<object?>(value);

    public string? ToInvokeString() => value;
}
