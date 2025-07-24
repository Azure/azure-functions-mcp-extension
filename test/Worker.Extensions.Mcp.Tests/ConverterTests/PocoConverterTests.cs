using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection.Converters;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class PocoConverterTests
{
    [Fact]
    public async Task ConvertAsync_ValidJson_ReturnsPoco()
    {
        var serializer = new JsonObjectSerializer();
        var converter = CreateConverter(serializer);
        var json = "{\"arguments\":{\"Name\":\"foo\"}}";
        var context = ConverterContextHelper.CreateConverterContext(typeof(TestPoco), json);

        ConversionResult result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var poco = Assert.IsType<TestPoco>(result.Value);
        Assert.Equal("foo", poco.Name);
    }

    [Fact]
    public async Task ConvertAsync_ContextIsNull_ThrowsArgumentNullException()
    {
        var converter = CreateConverter();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await converter.ConvertAsync(null!));
    }

    [Fact]
    public async Task ConvertAsync_TargetTypeString_ReturnsUnhandled()
    {
        var converter = CreateConverter();
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), "{\"arguments\":{}}");
        var result = await converter.ConvertAsync(context);
        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_TargetTypeToolInvocationContext_ReturnsUnhandled()
    {
        var converter = CreateConverter();
        var context = ConverterContextHelper.CreateConverterContext(typeof(ToolInvocationContext), "{\"arguments\":{}}");

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_SourceNotString_ReturnsUnhandled()
    {
        var converter = CreateConverter();
        var context = ConverterContextHelper.CreateConverterContext(typeof(TestPoco), 123);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_MissingArguments_ReturnsUnhandled()
    {
        var converter = CreateConverter();
        var json = "{\"notarguments\":{}}";
        var context = ConverterContextHelper.CreateConverterContext(typeof(TestPoco), json);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_DeserializeReturnsNull_ReturnsFailed()
    {
        var mockSerializer = new Mock<ObjectSerializer>();
        mockSerializer
            .Setup(s => s.DeserializeAsync(It.IsAny<Stream>(), typeof(TestPoco), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object)null!);
        var converter = CreateConverter(mockSerializer.Object);
        var json = "{\"arguments\":{\"Name\":\"foo\"}}";
        var context = ConverterContextHelper.CreateConverterContext(typeof(TestPoco), json);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    [Fact]
    public async Task ConvertAsync_InvalidJson_ReturnsFailed()
    {
        var converter = CreateConverter();
        var json = "{\"arguments\":{\"Name\":\"foo\""; // Invalid JSON
        var context = ConverterContextHelper.CreateConverterContext(typeof(TestPoco), json);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsAssignableFrom<JsonException>(result.Error);
    }

    private static PocoConverter CreateConverter(ObjectSerializer? serializer = null)
    {
        var options = Options.Create(new WorkerOptions { Serializer = serializer ?? new JsonObjectSerializer() });
        return new PocoConverter(options);
    }

    public class TestPoco
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
