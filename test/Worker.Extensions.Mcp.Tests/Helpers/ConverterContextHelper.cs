using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Moq;

namespace Worker.Extensions.Mcp.Tests.Helpers;

public static class ConverterContextHelper
{
    public static ConverterContext CreateConverterContext(
        Type targetType,
        BindingAttribute? bindingAttribute = null,
        FunctionContext? functionContext = null,
        Dictionary<string, object>? properties = null)
    {
        functionContext ??= Mock.Of<FunctionContext>();
        properties ??= new Dictionary<string, object>();

        // If binding attribute is provided, add it to the properties bag
        if (bindingAttribute is not null)
        {
            properties["bindingAttribute"] = bindingAttribute;
        }

        return new TestConverterContext(targetType, string.Empty, functionContext, properties);
    }

    public class TestConverterContext : ConverterContext
    {
        public TestConverterContext(
            Type targetType,
            object source,
            FunctionContext functionContext,
            IReadOnlyDictionary<string, object> properties)
        {
            TargetType = targetType;
            Source = source;
            FunctionContext = functionContext;
            Properties = properties;
        }

        public override Type TargetType { get; }
        public override object? Source { get; }
        public override FunctionContext FunctionContext { get; }
        public override IReadOnlyDictionary<string, object> Properties { get; }
    }
}
