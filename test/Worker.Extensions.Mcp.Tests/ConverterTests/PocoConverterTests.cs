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
        Assert.IsType<ArgumentNullException>(result.Error);
    }

    [Fact]
    public async Task ConvertAsync_WithEnumValue_SuccessfullyConverts()
    {
        var converter = new PocoConverter();
        var arguments = new Dictionary<string, object>
        {
            { "Status", "Active" }
        };

        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = arguments
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(PocoWithEnum), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var poco = Assert.IsType<PocoWithEnum>(result.Value);
        Assert.Equal(Status.Active, poco.Status);
    }

    [Fact]
    public async Task ConvertAsync_WithNullableInt_SuccessfullyConverts()
    {
        var converter = new PocoConverter();
        var arguments = new Dictionary<string, object>
        {
            { "Age", 30 }
        };

        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = arguments
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(PocoWithNullable), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var poco = Assert.IsType<PocoWithNullable>(result.Value);
        Assert.Equal(30, poco.Age);
    }

    [Fact]
    public async Task ConvertAsync_PropertyIsMissingOrReadOnly_IgnoresAndContinues()
    {
        var converter = new PocoConverter();
        var arguments = new Dictionary<string, object>
        {
            { "ReadOnlyProperty", "test" },
            { "NonExistent", 123 }
        };

        var toolContext = new ToolInvocationContext
        {
            Name = "test",
            Arguments = arguments
        };

        var functionContext = CreateFunctionContextWithToolContext(toolContext);
        var context = CreateConverterContext(typeof(PocoWithReadOnly), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.IsType<PocoWithReadOnly>(result.Value);
    }

    private class TestPoco
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }

    private class PocoWithEnum
    {
        public Status Status { get; set; }
    }

    private class PocoWithNullable
    {
        public int? Age { get; set; }
    }

    private class PocoWithReadOnly
    {
        public string ReadOnlyProperty => "readonly";
    }

    private enum Status
    {
        Inactive,
        Active
    }
}
