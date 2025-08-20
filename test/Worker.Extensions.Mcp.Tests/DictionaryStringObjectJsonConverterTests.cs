using System.Numerics;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.JsonConverters;

namespace Worker.Extensions.Mcp.Tests;

public class DictionaryStringObjectJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DictionaryStringObjectJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() },
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public void Deserialize_SimpleObject_ReturnsDictionary()
    {
        string json = """
        {
            "Name": "Test",
            "Age": 42,
            "IsActive": true,
            "Score": 99.5,
            "NullValue": null
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

        Assert.NotNull(result);
        Assert.Equal("Test", result["Name"]);
        Assert.Equal(42, result["Age"]);
        Assert.Equal(true, result["IsActive"]);
        Assert.Equal(99.5m, result["Score"]);
        Assert.Null(result["NullValue"]);
    }

    [Fact]
    public void Deserialize_NestedObject_ReturnsNestedDictionary()
    {
        string json = """
        {
            "User": {
                "Id": 123,
                "Name": "Alice"
            }
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

        var nested = Assert.IsType<Dictionary<string, object>>(result?["User"]);
        Assert.Equal(123, nested["Id"]);
        Assert.Equal("Alice", nested["Name"]);
    }

    [Fact]
    public void Deserialize_ArrayWithMixedTypes_ReturnsList()
    {
        string json = """
        {
            "Items": ["text", 1, 2.5, true, null]
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);
        var list = Assert.IsType<List<object>>(result?["Items"]);
        Assert.Equal("text", list[0]);
        Assert.Equal(1, list[1]);
        Assert.Equal(2.5m, list[2]);
        Assert.Equal(true, list[3]);
        Assert.Null(list[4]);
    }

    [Fact]
    public void Deserialize_AllJsonValueKinds_AreHandledCorrectly()
    {
        string json = """
        {
            "StringValue": "hello",
            "NumberValue": 123,
            "DateTimeValue": "2025-08-22",
            "DateTimeOffsetValue": "2025-08-22T14:30:00.0000000-07:00",
            "GuidValue": "d9b1f8c2-3e4f-4b5a-8c6d-7e8f9a0b1c2d",
            "DoubleValue": 123.45,
            "TrueValue": true,
            "FalseValue": false,
            "NullValue": null,
            "ObjectValue": {
                "Inner": "value"
            },
            "ArrayValue": [1, "two", false]
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

        Assert.NotNull(result);
        Assert.Equal("hello", result["StringValue"]);
        Assert.Equal(123, result["NumberValue"]);
        Assert.Equal(123.45m, result["DoubleValue"]);
        Assert.Equal(true, result["TrueValue"]);
        Assert.Equal(false, result["FalseValue"]);
        Assert.Null(result["NullValue"]);
        Assert.IsType<DateTimeOffset>(result["DateTimeValue"]);
        Assert.IsType<DateTimeOffset>(result["DateTimeOffsetValue"]);
        Assert.IsType<Guid>(result["GuidValue"]);

        var obj = Assert.IsType<Dictionary<string, object>>(result["ObjectValue"]);
        Assert.Equal("value", obj["Inner"]);

        var array = Assert.IsType<List<object>>(result["ArrayValue"]);
        Assert.Equal(3, array.Count);
        Assert.Equal(1, array[0]);
        Assert.Equal("two", array[1]);
        Assert.Equal(false, array[2]);
    }

    [Fact]
    public void Serialize_Dictionary_WritesValidJson()
    {
        var input = new Dictionary<string, object>
        {
            ["Name"] = "Test",
            ["Age"] = 25L,
            ["Nested"] = new Dictionary<string, object>
            {
                ["Flag"] = true
            }
        };

        string json = JsonSerializer.Serialize(input, _options);

        var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);
        Assert.Equal("Test", parsed?["Name"]);
        Assert.Equal(25, parsed?["Age"]);

        var nested = Assert.IsType<Dictionary<string, object>>(parsed?["Nested"]);
        Assert.Equal(true, nested["Flag"]);
    }

    [Fact]
    public void Deserialize_IntegerAndFloatingPointDifferentiation()
    {
        string json = """
        {
            "Int": 42,
            "FloatLikeInt": 42.0,
            "NegativeInt": -7,
            "NegativeDouble": -7.25
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;

        Assert.IsType<int>(result["Int"]);
        Assert.Equal(42, result["Int"]);

        Assert.IsType<decimal>(result["FloatLikeInt"]);
        Assert.Equal(42m, result["FloatLikeInt"]);

        Assert.IsType<int>(result["NegativeInt"]);
        Assert.Equal(-7, result["NegativeInt"]);

        Assert.IsType<decimal>(result["NegativeDouble"]);
        Assert.Equal(-7.25m, result["NegativeDouble"]);
    }

    [Fact]
    public void Deserialize_LargeNumbers_ClassifiedCorrectly()
    {
        long max = long.MaxValue;
        string json = $"{{\n  \"MaxLong\": {max},\n  \"BeyondLong\": {((BigInteger)max + 1).ToString()},\n  \"Scientific\": 1e6\n}}";

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;

        Assert.Equal(max, result["MaxLong"]);
        Assert.IsType<long>(result["MaxLong"]);

        Assert.IsType<decimal>(result["BeyondLong"]);

        Assert.IsType<decimal>(result["Scientific"]);
        Assert.Equal(1_000_000m, result["Scientific"]);
    }

    [Fact]
    public void Deserialize_CaseInsensitiveKeys_DictionaryUsesOrdinalIgnoreCase()
    {
        string json = """
        {
            "Lower": 1,
            "MIXED": 2
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;

        Assert.Equal(1, result["lower"]);
        Assert.Equal(2, result["mixed"]);
        Assert.Equal(result["Lower"], result["LOWER"]);
        Assert.Equal(result["MIXED"], result["mixed"]);
    }

    [Fact]
    public void Deserialize_ComplexNestedArraysAndObjects()
    {
        string json = """
        {
            "Data": [
                { "Id": 1, "Tags": ["a", "b"] },
                { "Id": 2, "Tags": [] },
                { "Id": 3, "Tags": [1, 2, 3] }
            ]
        }
        """;

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var data = Assert.IsType<List<object>>(result["Data"]);
        Assert.Equal(3, data.Count);

        var first = Assert.IsType<Dictionary<string, object>>(data[0]);
        Assert.Equal(1, first["Id"]);
        var firstTags = Assert.IsType<List<object>>(first["Tags"]);
        Assert.Equal(new[] { "a", "b" }, firstTags.Cast<string>());

        var second = Assert.IsType<Dictionary<string, object>>(data[1]);
        var secondTags = Assert.IsType<List<object>>(second["Tags"]);
        Assert.Empty(secondTags);

        var third = Assert.IsType<Dictionary<string, object>>(data[2]);
        var thirdTags = Assert.IsType<List<object>>(third["Tags"]);
        Assert.Equal(new object[] { 1, 2, 3 }, thirdTags);
    }

    [Fact]
    public void Serialize_ComplexStructure_RoundTrips()
    {
        var original = new Dictionary<string, object>
        {
            ["List"] = new List<object>
            {
                1L,
                2.5d,
                true,
                null!,
                new Dictionary<string, object> { ["Inner"] = "value" }
            },
            ["InnerDict"] = new Dictionary<string, object>
            {
                ["Numbers"] = new List<object> { 10L, 20L }
            }
        };

        string json = JsonSerializer.Serialize(original, _options);
        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;

        var list = Assert.IsType<List<object>>(roundTripped["List"]);
        Assert.Equal(5, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2.5m, list[1]);
        Assert.Equal(true, list[2]);
        Assert.Null(list[3]);
        var inner = Assert.IsType<Dictionary<string, object>>(list[4]);
        Assert.Equal("value", inner["Inner"]);

        var innerDict = Assert.IsType<Dictionary<string, object>>(roundTripped["InnerDict"]);
        var numbers = Assert.IsType<List<object>>(innerDict["Numbers"]);
        Assert.Equal(new object[] { 10, 20 }, numbers);
    }

    [Fact]
    public void Deserialize_RootNull_ReturnsNull()
    {
        string json = "null";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);
        Assert.Null(result);
    }

    // -------- Additional Tests --------

    [Fact]
    public void Deserialize_EmptyObject_Works()
    {
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>("{}", _options);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Deserialize_NestedEmptyObject_Works()
    {
        var json = """{ "Outer": { "Inner": {} } }""";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var outer = Assert.IsType<Dictionary<string, object>>(result["Outer"]);
        var inner = Assert.IsType<Dictionary<string, object>>(outer["Inner"]);
        Assert.Empty(inner);
    }

    [Fact]
    public void Deserialize_EmptyArray_Works()
    {
        var json = """{ "Items": [] }""";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var items = Assert.IsType<List<object>>(result["Items"]);
        Assert.Empty(items);
    }

    [Fact]
    public void Deserialize_DuplicateCaseInsensitiveKeys_LastWins()
    {
        var json = """{ "Name": "First", "name": "Second", "NAME": "Third" }""";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        Assert.Equal("Third", result["name"]);
    }

    [Fact]
    public void Deserialize_LargeIntegerBeyondSafeDouble_ShowsPrecisionLoss()
    {
        var json = """{ "Big": 9007199254740993 }"""; // 2^53 + 1
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var value = Assert.IsType<long>(result["Big"]);
        Assert.Equal(9007199254740992d, value);
    }

    [Fact]
    public void Deserialize_ScientificNotation_PositiveAndNegativeExponent()
    {
        var json = """{ "Pos": 1.23e5, "Neg": 4.56e-3 }""";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        Assert.Equal(123000m, result["Pos"]);
        Assert.Equal(0.00456m, result["Neg"]);
    }

    [Fact]
    public void Deserialize_NegativeZeroRepresentations()
    {
        var json = """{ "NegZero1": -0, "NegZero2": -0.0 }""";
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        Assert.Equal(0, result["NegZero1"]);
        Assert.Equal(0m, result["NegZero2"]);
    }

    [Fact]
    public void Deserialize_ArrayOfObjects_MixedForms()
    {
        var json = """
        {
            "Items": [
                {},
                { "A": 1 },
                { "B": [ true, false, null ] }
            ]
        }
        """;
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var list = Assert.IsType<List<object>>(result["Items"]);
        Assert.Equal(3, list.Count);
        Assert.IsType<Dictionary<string, object>>(list[0]);
        var second = Assert.IsType<Dictionary<string, object>>(list[1]);
        Assert.Equal(1, second["A"]);
        var third = Assert.IsType<Dictionary<string, object>>(list[2]);
        var bArr = Assert.IsType<List<object>>(third["B"]);
        Assert.Equal(new object?[] { true, false, null }, bArr);
    }

    [Fact]
    public void Deserialize_DeepNesting_WorksWithinLimits()
    {
        int depth = 30;
        var sb = new StringBuilder();
        for (int i = 0; i < depth; i++) sb.Append("{\"L\":");
        sb.Append("\"end\"");
        for (int i = 0; i < depth; i++) sb.Append('}');
        var json = sb.ToString();

        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;
        var current = result;
        for (int i = 0; i < depth - 1; i++)
        {
            current = Assert.IsType<Dictionary<string, object>>(current["L"]);
        }
        Assert.Equal("end", current["L"]);
    }

    [Fact]
    public void Serialize_Cycle_DirectDictionary_Throws()
    {
        var dict = new Dictionary<string, object>();
        dict["self"] = dict;
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(dict, _options));
        Assert.Contains("Cycle detected", ex.Message);
    }

    [Fact]
    public void Serialize_Cycle_IndirectThroughList_Throws()
    {
        var dict = new Dictionary<string, object>();
        var list = new List<object>();
        list.Add(dict);
        dict["list"] = list;
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(dict, _options));
        Assert.Contains("Cycle detected", ex.Message);
    }

    private sealed class CustomPayload
    {
        public int X { get; set; }
        public string? Label { get; set; }
    }

    [Fact]
    public void Serialize_CustomType_FallbackHandled()
    {
        var dict = new Dictionary<string, object>
        {
            ["Payload"] = new CustomPayload { X = 7, Label = "ok" }
        };

        var json = JsonSerializer.Serialize(dict, _options);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("Payload", out var payload));
        Assert.Equal(7, payload.GetProperty("X").GetInt32());
        Assert.Equal("ok", payload.GetProperty("Label").GetString());
    }

    [Fact]
    public void Deserialize_RootArray_Throws()
    {
        var json = "[1,2,3]";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options));
    }

    [Fact]
    public void Deserialize_RootPrimitive_Throws()
    {
        var json = "\"hello\"";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options));
    }

    [Fact]
    public void RoundTrip_PreservesPrimitiveTypes()
    {
        var original = new Dictionary<string, object>
        {
            ["Int"] = 5L,
            ["Double"] = 3.14d,
            ["Bool"] = true,
            ["String"] = "abc",
            ["Null"] = null!,
            ["Array"] = new List<object?> { 1L, 2.0d, null, "x" }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options)!;

        Assert.Equal(5, result["Int"]);
        Assert.Equal(3.14m, result["Double"]);
        Assert.Equal(true, result["Bool"]);
        Assert.Equal("abc", result["String"]);
        Assert.Null(result["Null"]);
        var arr = Assert.IsType<List<object>>(result["Array"]);
        Assert.Equal(new object?[] { 1, 2, null, "x" }, arr);
    }
}
