using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Worker.Extensions.Mcp.Tests.Helpers;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class ToolInvocationContextConverterTests
{
    [Fact]
    public async Task ConvertAsync_ValidToolInvocationContext_ReturnsSuccess()
    {
        var converter = new ToolInvocationContextConverter();
        var toolContext = new ToolInvocationContext
        {
            Name = "SayHello",
            Arguments = new Dictionary<string, object> { { "name", "friend" } }
        };

        var functionContext = FunctionContextHelper.CreateFunctionContextWithToolContext(toolContext);
        var context = ConverterContextHelper.CreateConverterContext(typeof(ToolInvocationContext), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var returned = Assert.IsType<ToolInvocationContext>(result.Value);
        Assert.Equal("SayHello", returned.Name);
        Assert.Equal("friend", returned?.Arguments?["name"]);
    }

    [Fact]
    public async Task ConvertAsync_TargetTypeIsNotToolInvocationContext_ReturnsUnhandled()
    {
        var converter = new ToolInvocationContextConverter();
        var functionContext = FunctionContextHelper.CreateFunctionContextWithToolContext(new ToolInvocationContext { Name = "TestName" });
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_ToolInvocationContextNotFound_ReturnsFailed()
    {
        var converter = new ToolInvocationContextConverter();
        var functionContext = FunctionContextHelper.CreateEmptyFunctionContext();
        var context = ConverterContextHelper.CreateConverterContext(typeof(ToolInvocationContext), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
    }
}
