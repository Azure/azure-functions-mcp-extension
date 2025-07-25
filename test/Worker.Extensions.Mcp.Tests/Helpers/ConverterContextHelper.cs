using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Moq;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public static class ConverterContextHelper
{
    public static ConverterContext CreateConverterContext(Type targetType, object source, FunctionContext? functionContext = null, Dictionary<string, object>? properties = null)
    {
        functionContext ??= Mock.Of<FunctionContext>();
        properties ??= new Dictionary<string, object>();
        return new TestConverterContext(targetType, source, functionContext, properties);
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

