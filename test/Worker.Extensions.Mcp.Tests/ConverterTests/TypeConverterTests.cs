using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Moq;
using static Worker.Extensions.Mcp.Tests.Helpers.ConverterContextHelper;
using static Worker.Extensions.Mcp.Tests.Helpers.FunctionContextHelper;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class TypeConverterTests
{
    [Theory]
    [MemberData(nameof(SupportedTypesTestCases))]
    public async Task ConvertAsync_SupportedTypes_ReturnsSuccess(Type targetType, object inputValue, object expectedValue)
    {
        var converter = new TypeConverter();
        var arguments = new Dictionary<string, object> { { "value", inputValue } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(targetType, null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal(expectedValue, result.Value);
    }

    public static IEnumerable<object[]> SupportedTypesTestCases()
    {
        yield return new object[] { typeof(string), "hello", "hello" };
        yield return new object[] { typeof(string), JsonSerializer.SerializeToElement("hello"), "hello" };

        yield return new object[] { typeof(int), 42, 42 };
        yield return new object[] { typeof(int), JsonSerializer.SerializeToElement(42), 42 };

        yield return new object[] { typeof(long), 123L, 123L };
        yield return new object[] { typeof(long), JsonSerializer.SerializeToElement(123L), 123L };

        yield return new object[] { typeof(float), 1.23f, 1.23f };
        yield return new object[] { typeof(float), JsonSerializer.SerializeToElement(1.23f), 1.23f };

        yield return new object[] { typeof(double), 3.14, 3.14 };
        yield return new object[] { typeof(double), JsonSerializer.SerializeToElement(3.14), 3.14 };

        yield return new object[] { typeof(decimal), 99.99m, 99.99m };
        yield return new object[] { typeof(decimal), JsonSerializer.SerializeToElement(99.99m), 99.99m };

        yield return new object[] { typeof(bool), true, true };
        yield return new object[] { typeof(bool), false, false };
        yield return new object[] { typeof(bool), JsonSerializer.SerializeToElement(true), true };
        yield return new object[] { typeof(bool), JsonSerializer.SerializeToElement(false), false };

        yield return new object[] { typeof(string), JsonSerializer.SerializeToElement(""), "" };

        yield return new object[] { typeof(int?), JsonDocument.Parse("null").RootElement, null! };
        yield return new object[] { typeof(string), JsonDocument.Parse("null").RootElement, null! };
    }

    [Fact]
    public async Task ConvertAsync_ContextIsNull_ThrowsArgumentNullException()
    {
        var converter = new TypeConverter();
        await Assert.ThrowsAsync<ArgumentNullException>(() => converter.ConvertAsync(null!).AsTask());
    }

    [Fact]
    public async Task ConvertAsync_ToolInvocationContextMissing_ReturnsUnhandled()
    {
        var converter = new TypeConverter();
        var functionContext = CreateEmptyFunctionContext();
        var context = CreateConverterContext(typeof(string), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ArgumentsCountIsZero_ReturnsUnhandled()
    {
        var converter = new TypeConverter();
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = new Dictionary<string, object>() };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(string), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ArgumentsCountMoreThanOne_ReturnsUnhandled()
    {
        var converter = new TypeConverter();
        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = new Dictionary<string, object>
            {
                { "a", 1 },
                { "b", 2 }
            }
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(int), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_TypeMismatch_ReturnsUnhandled()
    {
        var converter = new TypeConverter();
        var arguments = new Dictionary<string, object> { { "value", 123 } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };
        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(string), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }
}
