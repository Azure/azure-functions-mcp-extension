using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Moq;
using static Worker.Extensions.Mcp.Tests.Helpers.ConverterContextHelper;
using static Worker.Extensions.Mcp.Tests.Helpers.FunctionContextHelper;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class PocoConverterTests
{
    [Fact]
    public async Task ConvertAsync_ValidArguments_ReturnsPoco()
    {
        var converter = new PocoConverter();
        var arguments = new Dictionary<string, object> { { "Name", "foo" }, { "Description", "bar" } };
        var toolContext = new ToolInvocationContext { Name = "test", Arguments = arguments };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var converterContext = CreateConverterContext(typeof(TestPoco), null, functionContext);

        var result = await converter.ConvertAsync(converterContext);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var poco = Assert.IsType<TestPoco>(result.Value);
        Assert.Equal("foo", poco.Name);
        Assert.Equal("bar", poco.Description);
    }

    [Fact]
    public async Task ConvertAsync_ContextIsNull_ThrowsArgumentNullException()
    {
        var converter = new PocoConverter();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await converter.ConvertAsync(null!));
    }

    [Fact]
    public async Task ConvertAsync_TargetTypeString_ReturnsUnhandled()
    {
        var converter = new PocoConverter();
        var functionContext = new Mock<FunctionContext>();
        var context = CreateConverterContext(typeof(string), null, functionContext.Object);
        var result = await converter.ConvertAsync(context);
        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_TargetTypeToolInvocationContext_ReturnsUnhandled()
    {
        var converter = new PocoConverter();
        var functionContext = new Mock<FunctionContext>();
        var context = CreateConverterContext(typeof(ToolInvocationContext), null, functionContext.Object);
        var result = await converter.ConvertAsync(context);
        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ToolInvocationContextNotFound_ReturnsUnhandled()
    {
        var converter = new PocoConverter();
        var functionContext = CreateEmptyFunctionContext();
        var context = CreateConverterContext(typeof(TestPoco), null, functionContext);
        var result = await converter.ConvertAsync(context);
        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ArgumentsNull_ReturnsFailed()
    {
        var converter = new PocoConverter();

        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = null
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(TestPoco), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    [Fact]
    public async Task ConvertAsync_DeserializeThrowsJsonException_ReturnsFailed()
    {
        var converter = new PocoConverter();

        var arguments = new Dictionary<string, object>
        {
            { "Name", new object() }
        };

        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = arguments
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(TestPoco), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsAssignableFrom<JsonException>(result.Error);
    }

    private class TestPoco
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
