// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class MetadataExtractorTests
{
    [Fact]
    public void BuildMetadataJson_SingleFlatKey_ReturnsSimpleJson()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1")
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.Equal("value1", json["key1"]!.GetValue<string>());
    }

    [Fact]
    public void BuildMetadataJson_MultipleFlatKeys_ReturnsAllKeys()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", "value2"),
            new("key3", "value3")
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.Equal("value1", json["key1"]!.GetValue<string>());
        Assert.Equal("value2", json["key2"]!.GetValue<string>());
        Assert.Equal("value3", json["key3"]!.GetValue<string>());
    }

    [Fact]
    public void BuildMetadataJson_NestedKeyWithColon_CreatesNestedStructure()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("ui:resourceUri", "test-uri")
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.True(json.ContainsKey("ui"));
        var ui = json["ui"]!.AsObject();
        Assert.Equal("test-uri", ui["resourceUri"]!.GetValue<string>());
    }

    [Fact]
    public void BuildMetadataJson_DeeplyNestedKey_CreatesDeepStructure()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("level1:level2:level3", "deep-value")
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        var level1 = json["level1"]!.AsObject();
        var level2 = level1["level2"]!.AsObject();
        Assert.Equal("deep-value", level2["level3"]!.GetValue<string>());
    }

    [Fact]
    public void BuildMetadataJson_MultipleNestedKeysWithSharedParent_MergesCorrectly()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("file:version", "1.0.0"),
            new("file:releaseDate", "2024-01-01"),
            new("author", "John Doe")
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.Equal("John Doe", json["author"]!.GetValue<string>());
        var file = json["file"]!.AsObject();
        Assert.Equal("1.0.0", file["version"]!.GetValue<string>());
        Assert.Equal("2024-01-01", file["releaseDate"]!.GetValue<string>());
    }

    [Fact]
    public void BuildMetadataJson_NullValue_SerializesAsNull()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("nullKey", null)
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.True(json.ContainsKey("nullKey"));
        Assert.Null(json["nullKey"]);
    }

    [Fact]
    public void BuildMetadataJson_EmptyList_ReturnsEmptyObject()
    {
        var metadata = new List<KeyValuePair<string, object?>>();

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.Empty(json);
    }

    [Fact]
    public void BuildMetadataJson_IntegerValue_SerializesCorrectly()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("count", 42)
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.Equal(42, json["count"]!.GetValue<int>());
    }

    [Fact]
    public void BuildMetadataJson_BooleanValue_SerializesCorrectly()
    {
        var metadata = new List<KeyValuePair<string, object?>>
        {
            new("enabled", true)
        };

        var result = MetadataExtractor.BuildMetadataJson(metadata);
        var json = JsonNode.Parse(result)!.AsObject();

        Assert.True(json["enabled"]!.GetValue<bool>());
    }

    [Fact]
    public void TryExtractMetadataFromParameter_NoTriggerAttribute_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.NoTrigger))!;
        var parameters = method.GetParameters();

        var result = MetadataExtractor.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadata);

        Assert.False(result);
        Assert.Null(metadata);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithoutMetadata_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerNoMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataExtractor.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadata);

        Assert.False(result);
        Assert.Null(metadata);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithSingleMetadata_ReturnsMetadata()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithSingleMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataExtractor.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadata);

        Assert.True(result);
        Assert.NotNull(metadata);
        Assert.Single(metadata);
        Assert.Equal("author", metadata[0].Key);
        Assert.Equal("John", metadata[0].Value);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithMultipleMetadata_ReturnsAllMetadata()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithMultipleMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataExtractor.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadata);

        Assert.True(result);
        Assert.NotNull(metadata);
        Assert.Equal(3, metadata.Count);
    }

    private class TestClass
    {
        public void NoTrigger(string value) { }

        public void TriggerNoMetadata(
            [McpResourceTrigger("file://test", "test")] ResourceInvocationContext context) { }

        public void TriggerWithSingleMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("author", "John")] ResourceInvocationContext context) { }

        public void TriggerWithMultipleMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("author", "John")]
            [McpMetadata("version", "1.0")]
            [McpMetadata("date", "2024-01-01")] ResourceInvocationContext context) { }
    }
}
