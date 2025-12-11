// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpMetadataUtilityTests
{
    [Fact]
    public void ToJsonObject_WithNullMetadata_ReturnsNull()
    {
        var result = McpMetadataUtility.ToJsonObject(null);

        Assert.Null(result);
    }

    [Fact]
    public void ToJsonObject_WithEmptyMetadata_ReturnsNull()
    {
        var metadata = Array.Empty<IMcpResourceMetadata>();

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.Null(result);
    }

    [Fact]
    public void ToJsonObject_WithSimpleString_CreatesJsonValue()
    {
        var metadata = new[]
        {
            new TestMetadata("key1", "value1")
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("value1", result["key1"]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithBoolean_CreatesJsonValue()
    {
        var metadata = new[]
        {
            new TestMetadata("enabled", true),
            new TestMetadata("disabled", false)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result["enabled"]?.GetValue<bool>());
        Assert.False(result["disabled"]?.GetValue<bool>());
    }

    [Fact]
    public void ToJsonObject_WithNumber_CreatesJsonValue()
    {
        var metadata = new[]
        {
            new TestMetadata("count", 42),
            new TestMetadata("price", 19.99)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(42, result["count"]?.GetValue<int>());
        Assert.Equal(19.99, result["price"]?.GetValue<double>());
    }

    [Fact]
    public void ToJsonObject_WithNullValue_CreatesNullJsonNode()
    {
        var metadata = new[]
        {
            new TestMetadata("nullKey", null)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result["nullKey"]);
    }

    [Fact]
    public void ToJsonObject_WithJsonObjectString_ParsesAsObject()
    {
        var jsonString = """
        {
            "connect_domains": ["https://chatgpt.com"],
            "resource_domains": ["https://*.oaistatic.com"]
        }
        """;
        var metadata = new[]
        {
            new TestMetadata("openai/widgetCSP", jsonString)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        var csp = result["openai/widgetCSP"] as JsonObject;
        Assert.NotNull(csp);
        Assert.NotNull(csp["connect_domains"]);
        Assert.NotNull(csp["resource_domains"]);
    }

    [Fact]
    public void ToJsonObject_WithJsonArrayString_ParsesAsArray()
    {
        var jsonString = """["item1", "item2", "item3"]""";
        var metadata = new[]
        {
            new TestMetadata("items", jsonString)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        var array = result["items"] as JsonArray;
        Assert.NotNull(array);
        Assert.Equal(3, array.Count);
        Assert.Equal("item1", array[0]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithInvalidJsonString_TreatsAsRegularString()
    {
        var invalidJson = "{this is not valid json}";
        var metadata = new[]
        {
            new TestMetadata("data", invalidJson)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(invalidJson, result["data"]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithStringStartingWithBrace_ButNotJson_TreatsAsString()
    {
        var notJson = "{ hello world";
        var metadata = new[]
        {
            new TestMetadata("message", notJson)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(notJson, result["message"]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithPreConstructedJsonNode_UsesDirectly()
    {
        var jsonObject = new JsonObject
        {
            ["nested"] = "value"
        };
        var metadata = new[]
        {
            new TestMetadata("config", jsonObject)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        var config = result["config"] as JsonObject;
        Assert.NotNull(config);
        Assert.Equal("value", config["nested"]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithMultipleMetadataItems_CreatesAllEntries()
    {
        var metadata = new[]
        {
            new TestMetadata("openai/closeWidget", true),
            new TestMetadata("openai/widgetDomain", "https://chatgpt.com"),
            new TestMetadata("openai/widgetCSP", """
            {
                "connect_domains": ["https://chatgpt.com"]
            }
            """)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result["openai/closeWidget"]?.GetValue<bool>());
        Assert.Equal("https://chatgpt.com", result["openai/widgetDomain"]?.GetValue<string>());
        Assert.NotNull(result["openai/widgetCSP"] as JsonObject);
    }

    [Fact]
    public void ToJsonObject_WithWhitespaceBeforeJson_StillParses()
    {
        var jsonString = "   \n\t  {\"key\": \"value\"}";
        var metadata = new[]
        {
            new TestMetadata("data", jsonString)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        var data = result["data"] as JsonObject;
        Assert.NotNull(data);
        Assert.Equal("value", data["key"]?.GetValue<string>());
    }

    [Fact]
    public void ToJsonObject_WithRegularStringNotStartingWithBrace_TreatsAsString()
    {
        var regularString = "just a regular string";
        var metadata = new[]
        {
            new TestMetadata("message", regularString)
        };

        var result = McpMetadataUtility.ToJsonObject(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(regularString, result["message"]?.GetValue<string>());
    }

    private class TestMetadata : IMcpResourceMetadata
    {
        public TestMetadata(string key, object? value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public object? Value { get; }
    }
}
