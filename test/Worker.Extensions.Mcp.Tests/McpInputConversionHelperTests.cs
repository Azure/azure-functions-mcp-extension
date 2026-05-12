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

    public static IEnumerable<object[]> DateTimeOffsetToDateTimeValues()
    {
        return [
            [new DateTimeOffset(2026, 4, 8, 12, 0, 0, TimeSpan.Zero), new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc)],
            [new DateTimeOffset(2026, 4, 8, 12, 0, 0, TimeSpan.FromHours(5)), new DateTime(2026, 4, 8, 7, 0, 0, DateTimeKind.Utc)],
        ];
    }

    public static IEnumerable<object[]> DateTimeToDateTimeOffsetValues()
    {
        return [
            [new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc), new DateTimeOffset(2026, 4, 8, 12, 0, 0, TimeSpan.Zero)],
            [new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Unspecified), new DateTimeOffset(2026, 4, 8, 12, 0, 0, TimeSpan.Zero)],
        ];
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetToDateTimeValues))]
    public void TryConvertToTargetType_DateTimeOffsetToDateTime_Success(DateTimeOffset input, DateTime expected)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(DateTime), out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
        Assert.Equal(DateTimeKind.Utc, ((DateTime)result!).Kind);
    }

    [Theory]
    [MemberData(nameof(DateTimeToDateTimeOffsetValues))]
    public void TryConvertToTargetType_DateTimeToDateTimeOffset_Success(DateTime input, DateTimeOffset expected)
    {
        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(DateTimeOffset), out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryConvertToTargetType_ArrayOfComplexType_PreservesElementValues()
    {
        var input = new List<object?>
        {
            new Dictionary<string, object>
            {
                ["Name"] = "Widget",
                ["Quantity"] = 3,
            },
            new Dictionary<string, object>
            {
                ["Name"] = "Gadget",
                ["Quantity"] = 7,
            },
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(Item[]), out var result);

        Assert.True(success);
        Assert.NotNull(result);
        var items = Assert.IsType<Item[]>(result);
        Assert.Equal(2, items.Length);

        Assert.NotNull(items[0]);
        Assert.Equal("Widget", items[0]!.Name);
        Assert.Equal(3, items[0]!.Quantity);

        Assert.NotNull(items[1]);
        Assert.Equal("Gadget", items[1]!.Name);
        Assert.Equal(7, items[1]!.Quantity);
    }

    [Fact]
    public void TryConvertToTargetType_ListOfComplexType_PreservesElementValues()
    {
        var input = BuildItemDictionaries();

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(List<Item>), out var result);

        Assert.True(success);
        var items = Assert.IsType<List<Item>>(result);
        AssertWidgetAndGadget(items);
    }

    [Fact]
    public void TryConvertToTargetType_IEnumerableOfComplexType_PreservesElementValues()
    {
        var input = BuildItemDictionaries();

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(IEnumerable<Item>), out var result);

        Assert.True(success);
        var items = (IEnumerable<Item>)result!;
        AssertWidgetAndGadget(items.ToList());
    }

    [Fact]
    public void TryConvertToTargetType_NestedPocoProperty_IsPopulated()
    {
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Alice",
            ["Address"] = new Dictionary<string, object>
            {
                ["Street"] = "123 Main",
                ["City"] = "Seattle",
            },
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(Person), out var result);

        Assert.True(success);
        var person = Assert.IsType<Person>(result);
        Assert.Equal("Alice", person.Name);
        Assert.NotNull(person.Address);
        Assert.Equal("123 Main", person.Address!.Street);
        Assert.Equal("Seattle", person.Address.City);
    }

    [Fact]
    public void TryConvertToTargetType_PocoWithArrayOfComplexProperty_IsPopulated()
    {
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Order #1",
            ["Items"] = BuildItemDictionaries(),
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(OrderWithArray), out var result);

        Assert.True(success);
        var order = Assert.IsType<OrderWithArray>(result);
        Assert.Equal("Order #1", order.Name);
        Assert.NotNull(order.Items);
        AssertWidgetAndGadget(order.Items!);
    }

    [Fact]
    public void TryConvertToTargetType_PocoWithListOfComplexProperty_IsPopulated()
    {
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Order #2",
            ["Items"] = BuildItemDictionaries(),
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(OrderWithList), out var result);

        Assert.True(success);
        var order = Assert.IsType<OrderWithList>(result);
        Assert.Equal("Order #2", order.Name);
        Assert.NotNull(order.Items);
        AssertWidgetAndGadget(order.Items!);
    }

    [Fact]
    public void TryConvertToTargetType_PocoWithIEnumerableOfComplexProperty_IsPopulated()
    {
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Order #3",
            ["Items"] = BuildItemDictionaries(),
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(OrderWithEnumerable), out var result);

        Assert.True(success);
        var order = Assert.IsType<OrderWithEnumerable>(result);
        Assert.Equal("Order #3", order.Name);
        Assert.NotNull(order.Items);
        AssertWidgetAndGadget(order.Items!.ToList());
    }

    [Fact]
    public void TryConvertToTargetType_DeeplyNested_PocoAndArrayOfPoco_IsPopulated()
    {
        // Company -> Department[] -> Employee.Address (POCO)
        // Exercises: nested POCO inside an element of an array property, plus a POCO property
        // on that element. Proves the recursion works at arbitrary depth.
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Contoso",
            ["Departments"] = new List<object?>
            {
                new Dictionary<string, object>
                {
                    ["Name"] = "Engineering",
                    ["Lead"] = new Dictionary<string, object>
                    {
                        ["Name"] = "Alice",
                        ["Address"] = new Dictionary<string, object>
                        {
                            ["Street"] = "1 Infinite Loop",
                            ["City"] = "Cupertino",
                        },
                    },
                },
                new Dictionary<string, object>
                {
                    ["Name"] = "Sales",
                    ["Lead"] = new Dictionary<string, object>
                    {
                        ["Name"] = "Bob",
                        ["Address"] = new Dictionary<string, object>
                        {
                            ["Street"] = "350 5th Ave",
                            ["City"] = "New York",
                        },
                    },
                },
            },
        };

        var success = McpInputConversionHelper.TryConvertArgumentToTargetType(input, typeof(Company), out var result);

        Assert.True(success);
        var company = Assert.IsType<Company>(result);
        Assert.Equal("Contoso", company.Name);
        Assert.NotNull(company.Departments);
        Assert.Equal(2, company.Departments!.Length);

        var eng = company.Departments[0];
        Assert.Equal("Engineering", eng.Name);
        Assert.NotNull(eng.Lead);
        Assert.Equal("Alice", eng.Lead!.Name);
        Assert.NotNull(eng.Lead.Address);
        Assert.Equal("1 Infinite Loop", eng.Lead.Address!.Street);
        Assert.Equal("Cupertino", eng.Lead.Address.City);

        var sales = company.Departments[1];
        Assert.Equal("Sales", sales.Name);
        Assert.NotNull(sales.Lead);
        Assert.Equal("Bob", sales.Lead!.Name);
        Assert.NotNull(sales.Lead.Address);
        Assert.Equal("350 5th Ave", sales.Lead.Address!.Street);
        Assert.Equal("New York", sales.Lead.Address.City);
    }

    private static List<object?> BuildItemDictionaries() =>
    [
        new Dictionary<string, object>
        {
            ["Name"] = "Widget",
            ["Quantity"] = 3,
        },
        new Dictionary<string, object>
        {
            ["Name"] = "Gadget",
            ["Quantity"] = 7,
        },
    ];

    private static void AssertWidgetAndGadget(IReadOnlyList<Item> items)
    {
        Assert.Equal(2, items.Count);
        Assert.NotNull(items[0]);
        Assert.Equal("Widget", items[0]!.Name);
        Assert.Equal(3, items[0]!.Quantity);
        Assert.NotNull(items[1]);
        Assert.Equal("Gadget", items[1]!.Name);
        Assert.Equal(7, items[1]!.Quantity);
    }

    private class Person
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    private class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    private class OrderWithArray
    {
        public string? Name { get; set; }
        public Item[]? Items { get; set; }
    }

    private class OrderWithList
    {
        public string? Name { get; set; }
        public List<Item>? Items { get; set; }
    }

    private class OrderWithEnumerable
    {
        public string? Name { get; set; }
        public IEnumerable<Item>? Items { get; set; }
    }

    private class Item
    {
        public string? Name { get; set; }
        public int Quantity { get; set; }
    }

    private class Company
    {
        public string? Name { get; set; }
        public Department[]? Departments { get; set; }
    }

    private class Department
    {
        public string? Name { get; set; }
        public Employee? Lead { get; set; }
    }

    private class Employee
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
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
