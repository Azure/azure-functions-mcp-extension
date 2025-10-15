using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using static Worker.Extensions.Mcp.Tests.Helpers.ConverterContextHelper;
using static Worker.Extensions.Mcp.Tests.Helpers.FunctionContextHelper;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class ToolInvocationArgumentTypeConverterTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public async Task ConvertAsync_String_SupportedTypes_ReturnsSuccess(object? inputValue, object? expectedValue)
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", inputValue! } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var bindingAttribute = new McpToolPropertyAttribute("foo", "", false);
        var context = CreateConverterContext(typeof(string), bindingAttribute, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData(typeof(int), 42, 42)]
    [InlineData(typeof(long), 123L, 123L)]
    [InlineData(typeof(float), 1.23f, 1.23f)]
    [InlineData(typeof(double), 3.14, 3.14)]
    public async Task ConvertAsync_Number_SupportedTypes_ReturnsSuccess(Type targetType, object inputValue, object expectedValue)
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", inputValue! } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var bindingAttribute = new McpToolPropertyAttribute("foo", "", false);
        var context = CreateConverterContext(targetType, bindingAttribute, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ConvertAsync_Bool_SupportedTypes_ReturnsSuccess(object inputValue, object expectedValue)
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", inputValue! } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var bindingAttribute = new McpToolPropertyAttribute("foo", "", false);
        var context = CreateConverterContext(typeof(bool), bindingAttribute, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData("d2719f3e-8f5b-4c3a-9c1d-1e2f3a4b5c6d", typeof(Guid))]
    [InlineData("1995-10-13 14:45:32Z", typeof(DateTime))]
    [InlineData("2000-01-20 19:15:41Z", typeof(DateTimeOffset))]
    public async Task ConvertAsync_ValueAlreadyOfTargetType_ReturnsSuccess(object value, Type type)
    {
        object inputValue = CreateValueAsTargetType(value, type);

        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", inputValue! } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var bindingAttribute = new McpToolPropertyAttribute("foo", "", false);
        var context = CreateConverterContext(type, bindingAttribute, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(inputValue, result.Value);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ConvertAsync_WithMissingArgument_ReturnsUnhandled(object inputValue, object expectedValue)
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", inputValue! } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var bindingAttribute = new McpToolPropertyAttribute("foo", "", false);
        var context = CreateConverterContext(typeof(bool), bindingAttribute, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public async Task ConvertAsync_ContextIsNull_ThrowsArgumentNullException()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        await Assert.ThrowsAsync<ArgumentNullException>(() => converter.ConvertAsync(null!).AsTask());
    }

    [Fact]
    public async Task ConvertAsync_ToolInvocationContextMissing_ReturnsUnhandled()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var functionContext = CreateEmptyFunctionContext();
        var context = CreateConverterContext(typeof(string), new McpToolPropertyAttribute( "foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ArgumentsEmpty_ReturnsUnhandled()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = new Dictionary<string, object>() };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(string), new McpToolPropertyAttribute("foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_KeyNotFoundInArguments_ReturnsUnhandled()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "bar", 123 } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(int), bindingAttribute: new McpToolPropertyAttribute("foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_TypeMismatch_ReturnsUnhandled()
    {
        ToolInvocationArgumentTypeConverter converter = new();
        Dictionary<string, object> arguments = new() { { "foo", "test" } };
        ToolInvocationContext toolContext = new() { Name = "test", Arguments = arguments };
        FunctionContext functionContext = CreateFunctionContextWithToolContext(toolContext);
        ConverterContext context = CreateConverterContext(typeof(int), new McpToolPropertyAttribute("foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    private object CreateValueAsTargetType(object value, Type targetType)
    {
        return targetType switch
        {
            Type t when t == typeof(Guid) => Guid.Parse((string)value),
            Type t when t == typeof(DateTime) => DateTime.Parse((string)value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal),
            Type t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse((string)value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal),
            _ => throw new ArgumentException("Unsupported type", nameof(targetType)),
        };
    }
}
