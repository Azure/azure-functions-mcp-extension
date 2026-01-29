// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Extensions.Mcp;

namespace Extensions.Mcp.Tests;

public class MetadataParserTests
{
    #region ParseMetadata Tests

    [Fact]
    public void ParseMetadata_NullInput_ReturnsEmptyDictionary()
    {
        var result = MetadataParser.ParseMetadata(null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseMetadata_EmptyString_ReturnsEmptyDictionary()
    {
        var result = MetadataParser.ParseMetadata(string.Empty);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseMetadata_SimpleObject_ReturnsDictionary()
    {
        var json = """{"key1":"value1","key2":"value2"}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
    }

    [Fact]
    public void ParseMetadata_NestedObject_PreservesNesting()
    {
        var json = """{"openai/outputTemplate":"ui://widget/welcome.html","openai/widgetCSP":{"connect_domains":["array","of","domains"],"resource_domains":[]}}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("ui://widget/welcome.html", result["openai/outputTemplate"]);
        
        var nestedObj = result["openai/widgetCSP"] as IReadOnlyDictionary<string, object?>;
        Assert.NotNull(nestedObj);
        Assert.Equal(2, nestedObj.Count);
        
        var connectDomains = nestedObj["connect_domains"] as IList<object?>;
        Assert.NotNull(connectDomains);
        Assert.Equal(3, connectDomains.Count);
        Assert.Equal("array", connectDomains[0]);
        Assert.Equal("of", connectDomains[1]);
        Assert.Equal("domains", connectDomains[2]);
        
        var resourceDomains = nestedObj["resource_domains"] as IList<object?>;
        Assert.NotNull(resourceDomains);
        Assert.Empty(resourceDomains);
    }

    [Fact]
    public void ParseMetadata_WithArray_PreservesArrayValues()
    {
        var json = """{"items":["one","two","three"]}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal("one", items[0]);
        Assert.Equal("two", items[1]);
        Assert.Equal("three", items[2]);
    }

    [Fact]
    public void ParseMetadata_WithNumbers_ConvertsToCorrectType()
    {
        var json = """{"intValue":42,"doubleValue":3.14,"longValue":9223372036854775807}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.IsType<long>(result["intValue"]);
        Assert.Equal(42L, result["intValue"]);
        
        Assert.IsType<double>(result["doubleValue"]);
        Assert.Equal(3.14, (double)result["doubleValue"]!, 2);
        
        Assert.IsType<long>(result["longValue"]);
        Assert.Equal(9223372036854775807L, result["longValue"]);
    }

    [Fact]
    public void ParseMetadata_WithBooleans_ConvertsToBool()
    {
        var json = """{"enabled":true,"disabled":false}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.IsType<bool>(result["enabled"]);
        Assert.True((bool)result["enabled"]!);
        
        Assert.IsType<bool>(result["disabled"]);
        Assert.False((bool)result["disabled"]!);
    }

    [Fact]
    public void ParseMetadata_WithNull_ConvertsToNull()
    {
        var json = """{"nullValue":null,"stringValue":"test"}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.Null(result["nullValue"]);
        Assert.Equal("test", result["stringValue"]);
    }

    [Fact]
    public void ParseMetadata_CaseInsensitiveKeys_MaintainsOriginalCase()
    {
        var json = """{"KeyOne":"value1","keyTwo":"value2","KEYTHREE":"value3"}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Dictionary uses OrdinalIgnoreCase comparer, so lookups are case-insensitive
        Assert.Equal("value1", result["keyone"]);
        Assert.Equal("value2", result["keytwo"]);
        Assert.Equal("value3", result["keythree"]);
    }

    [Fact]
    public void ParseMetadata_WithNestedArrayOfObjects_ConvertsCorrectly()
    {
        var json = """{"resources":[{"name":"resource1","type":"type1"},{"name":"resource2","type":"type2"}]}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        var resources = result["resources"] as IList<object?>;
        Assert.NotNull(resources);
        Assert.Equal(2, resources.Count);
        
        var res1 = resources[0] as IReadOnlyDictionary<string, object?>;
        Assert.NotNull(res1);
        Assert.Equal("resource1", res1["name"]);
        Assert.Equal("type1", res1["type"]);
        
        var res2 = resources[1] as IReadOnlyDictionary<string, object?>;
        Assert.NotNull(res2);
        Assert.Equal("resource2", res2["name"]);
        Assert.Equal("type2", res2["type"]);
    }

    [Fact]
    public void ParseMetadata_InvalidJson_Throws()
    {
        var invalidJson = """{"invalid json}""";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => MetadataParser.ParseMetadata(invalidJson));
    }

    [Fact]
    public void ParseMetadata_JsonArray_ThrowsJsonException()
    {
        var json = """["one","two","three"]""";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => MetadataParser.ParseMetadata(json));
    }

    [Fact]
    public void ParseMetadata_JsonString_Throws()
    {
        var json = """"just a string"""";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => MetadataParser.ParseMetadata(json));
    }

    [Fact]
    public void ParseMetadata_EmptyObject_ReturnsEmptyDictionary()
    {
        var json = """{}""";

        var result = MetadataParser.ParseMetadata(json);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region SerializeMetadata Tests

    [Fact]
    public void SerializeMetadata_NullInput_ReturnsNull()
    {
        var result = MetadataParser.SerializeMetadata(null);

        Assert.Null(result);
    }

    [Fact]
    public void SerializeMetadata_EmptyDictionary_ReturnsNull()
    {
        var metadata = new Dictionary<string, object?>();

        var result = MetadataParser.SerializeMetadata(metadata);

        Assert.Null(result);
    }

    [Fact]
    public void SerializeMetadata_SimpleDictionary_ReturnsJsonObject()
    {
        var metadata = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var result = MetadataParser.SerializeMetadata(metadata);

        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        Assert.Equal("value1", result["key1"]?.GetValue<string>());
        Assert.Equal("value2", result["key2"]?.GetValue<string>());
    }

    [Fact]
    public void SerializeMetadata_NestedDictionary_PreservesNesting()
    {
        var metadata = new Dictionary<string, object?>
        {
            { "openai/outputTemplate", "ui://widget/welcome.html" },
            { "openai/widgetCSP", new Dictionary<string, object?>
            {
                { "connect_domains", new[] { "array", "of", "domains" } },
                { "resource_domains", new string[] { } }
            }}
        };

        var result = MetadataParser.SerializeMetadata(metadata);

        Assert.NotNull(result);
        Assert.NotNull(result["openai/outputTemplate"]);
        Assert.NotNull(result["openai/widgetCSP"]);
    }

    [Fact]
    public void SerializeMetadata_WithVariousTypes_SerializesCorrectly()
    {
        var metadata = new Dictionary<string, object?>
        {
            { "string", "value" },
            { "number", 42 },
            { "boolean", true },
            { "null", null },
            { "array", new[] { "one", "two" } },
            { "nested", new Dictionary<string, object?> { { "key", "value" } } }
        };

        var result = MetadataParser.SerializeMetadata(metadata);

        Assert.NotNull(result);
        Assert.Equal("value", result["string"]?.GetValue<string>());
        Assert.Equal(42, result["number"]?.GetValue<int>());
        Assert.True(result["boolean"]?.GetValue<bool>());
        Assert.Null(result["null"]);
    }

    #endregion
}
