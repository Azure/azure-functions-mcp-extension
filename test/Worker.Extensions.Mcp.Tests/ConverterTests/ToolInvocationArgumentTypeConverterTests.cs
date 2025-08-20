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
        var bindingAttribute = new McpToolPropertyAttribute("foo", "string", "", false);
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
        var bindingAttribute = new McpToolPropertyAttribute("foo", "number", "", false);
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
        var bindingAttribute = new McpToolPropertyAttribute("foo", "boolean", "", false);
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
        var context = CreateConverterContext(typeof(string), new McpToolPropertyAttribute("string", "foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ArgumentsEmpty_ReturnsUnhandled()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = new Dictionary<string, object>() };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(string), new McpToolPropertyAttribute("string", "foo", "", false), functionContext);

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
        var context = CreateConverterContext(typeof(int), bindingAttribute: new McpToolPropertyAttribute("int", "foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_TypeMismatch_ReturnsUnhandled()
    {
        var converter = new ToolInvocationArgumentTypeConverter();
        var arguments = new Dictionary<string, object> { { "foo", 123 } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(int), new McpToolPropertyAttribute("int", "foo", "", false), functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }
}
