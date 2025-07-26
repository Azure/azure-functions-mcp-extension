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
        Assert.Equal(42L, result["Age"]);
        Assert.Equal(true, result["IsActive"]);
        Assert.Equal(99.5d, result["Score"]);
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
        Assert.Equal(123L, nested["Id"]);
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
        Assert.Equal(1L, list[1]);
        Assert.Equal(2.5d, list[2]);
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
        Assert.Equal(123L, result["NumberValue"]);
        Assert.Equal(123.45d, result["DoubleValue"]);
        Assert.Equal(true, result["TrueValue"]);
        Assert.Equal(false, result["FalseValue"]);
        Assert.Null(result["NullValue"]);

        var obj = Assert.IsType<Dictionary<string, object>>(result["ObjectValue"]);
        Assert.Equal("value", obj["Inner"]);

        var array = Assert.IsType<List<object>>(result["ArrayValue"]);
        Assert.Equal(3, array.Count);
        Assert.Equal(1L, array[0]);
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
        Assert.Equal(25L, parsed?["Age"]);

        var nested = Assert.IsType<Dictionary<string, object>>(parsed?["Nested"]);
        Assert.Equal(true, nested["Flag"]);
    }
}
