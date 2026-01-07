// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Globalization;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class McpInputConversionHelperTests
{
    [Theory]
    [InlineData(null, typeof(string), null, true)]
    [InlineData(null, typeof(int?), null, true)]
    [InlineData(null, typeof(int), null, false)]
    [InlineData(null, typeof(StatusEnum), null, false)]
    [InlineData(null, typeof(StatusEnum?), null, true)]
    public void TryConvertToTargetType_NullValues(object? input, Type targetType, object? expected, bool expectedSuccess)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData(123, typeof(int), 123)]
    [InlineData("123", typeof(int?), 123)]
    [InlineData(123, typeof(int?), 123)]
    [InlineData("true", typeof(bool), true)]
    [InlineData(true, typeof(bool), true)]
    [InlineData(true, typeof(bool?), true)]
    [InlineData("3.14", typeof(double), 3.14)]
    [InlineData(123, typeof(string), "123")]
    public void TryConvertToTargetType_PrimitiveConversions(object input, Type targetType, object expected)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Active", typeof(StatusEnum), StatusEnum.Active)]
    [InlineData("inactive", typeof(StatusEnum), StatusEnum.Inactive)]
    public void TryConvertToTargetType_EnumConversion_Success(string input, Type targetType, object expected)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryConvertToTargetType_FailedConversion_ReturnsFalse()
    {
        var input = "not-a-number";

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(int), out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertToTargetType_InvalidEnum_Throws_ReturnsFalse()
    {
        var input = "Unknown";

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(StatusEnum), out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1, typeof(StatusEnum), StatusEnum.Active)]
    [InlineData(0, typeof(StatusEnum), StatusEnum.Inactive)]
    public void TryConvertToTargetType_EnumConversion_FromInteger(object input, Type targetType, object expected)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryConvertToTargetType_CustomTypeConverter_Success()
    {
        var input = "John,25";
        var targetType = typeof(TestPoco);

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.True(success);
        Assert.NotNull(result);
        Assert.IsType<TestPoco>(result);

        var poco = (TestPoco)result!;
        Assert.Equal("John", poco.Name);
        Assert.Equal(25, poco.Age);
    }

    [Fact]
    public void TryConvertToTargetType_InvalidFormat_ReturnsFalse()
    {
        var input = "not-a-number";
        var targetType = typeof(int);

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null, typeof(int?), null, true)]
    [InlineData(123, typeof(int?), 123, true)]
    [InlineData("invalid", typeof(int?), null, false)]
    public void TryConvertToTargetType_NullableValueTypes(object? input, Type targetType, object? expected, bool expectedSuccess)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, targetType, out var result);
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(StringConversionValues))]
    public void TryConvertToTargetType_SupportedTypeToString_Success(object input, string expectedResult)
    {
        var guid = Guid.NewGuid();
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(string), out var result);
        Assert.True(success);
        Assert.Equal(expectedResult, result);
    }

    public static IEnumerable<object[]> StringConversionValues()
    {
        return [
            [Guid.Parse("E3A69220-1099-483A-942F-4877CCB80E21"), "e3a69220-1099-483a-942f-4877ccb80e21"],
            [DateTime.Parse("01/01/2025"), "01/01/2025 00:00:00"],
            [DateTimeOffset.Parse("01/01/2025 00:00:00 +00:00"), "01/01/2025 00:00:00 +00:00"]
        ];
    }

    [TypeConverter(typeof(TestPocoConverter))]
    private class TestPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    internal class TestPocoConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                var parts = str.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out var age))
                {
                    return new TestPoco { Name = parts[0], Age = age };
                }
            }
            return null;
        }
    }

    private enum StatusEnum
    {
        Inactive = 0,
        Active = 1
    }
}
