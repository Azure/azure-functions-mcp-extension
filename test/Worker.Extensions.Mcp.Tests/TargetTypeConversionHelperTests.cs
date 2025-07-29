using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class TargetTypeConversionHelperTests
{
    [Theory]
    [InlineData(null, typeof(string), null, true)]
    [InlineData(null, typeof(int?), null, true)]
    [InlineData(null, typeof(int), null, false)]
    public void TryConvertToTargetType_NullValues(object? input, Type targetType, object? expected, bool expectedSuccess)
    {
        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, targetType, out var result);
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("3.14", typeof(double), 3.14)]
    [InlineData(123, typeof(string), "123")]
    public void TryConvertToTargetType_PrimitiveConversions(object input, Type targetType, object expected)
    {
        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Active", typeof(StatusEnum), StatusEnum.Active)]
    [InlineData("inactive", typeof(StatusEnum), StatusEnum.Inactive)]
    public void TryConvertToTargetType_EnumConversion_Success(string input, Type targetType, object expected)
    {
        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryConvertToTargetType_JsonConversionToPoco_Success()
    {
        var input = new Dictionary<string, object> { { "Name", "test" }, { "Age", 42 } };

        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, typeof(TestPoco), out var result);

        Assert.True(success);
        var poco = Assert.IsType<TestPoco>(result);
        Assert.Equal("test", poco.Name);
        Assert.Equal(42, poco.Age);
    }

    [Fact]
    public void TryConvertToTargetType_FailedConversion_ReturnsFalse()
    {
        var input = "not-a-number";

        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, typeof(int), out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertToTargetType_InvalidEnum_Throws_ReturnsFalse()
    {
        var input = "Unknown";

        var success = TargetTypeConversionHelper.TryConvertToTargetType(input, typeof(StatusEnum), out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    private class TestPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private enum StatusEnum
    {
        Inactive,
        Active
    }
}
