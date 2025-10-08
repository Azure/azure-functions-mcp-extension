// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolExtensionsTests
{
    private static IMcpTool CreateTool(params IMcpToolProperty[] properties)
    {
        var mock = new Mock<IMcpTool>();
        mock.SetupGet(t => t.Properties).Returns(properties);
        mock.SetupGet(t => t.Name).Returns("tool");
        mock.SetupProperty(t => t.Description, "desc");
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }

    private static IMcpToolProperty CreateProperty(string name, string type, string? description = null, bool required = false, bool isArray = false)
    {
        var mock = new Mock<IMcpToolProperty>();
        mock.SetupAllProperties();
        mock.Object.PropertyName = name;
        mock.Object.PropertyType = type;
        mock.Object.Description = description;
        mock.Object.IsRequired = required;
        mock.Object.IsArray = isArray;
        return mock.Object;
    }

    [Fact]
    public void GetPropertiesInputSchema_NoProperties_ReturnsEmptySchema()
    {
        var tool = CreateTool();

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Empty(schema.GetProperty("properties").EnumerateObject());
        Assert.Equal(0, schema.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public void GetPropertiesInputSchema_IncludesAllPropertiesAndRequiredArray()
    {
        var prop1 = CreateProperty("first", "string", "First property", required: true);
        var prop2 = CreateProperty("second", "number", null, required: false);
        var tool = CreateTool(prop1, prop2);

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");

        var first = properties.GetProperty("first");
        Assert.Equal("string", first.GetProperty("type").GetString());
        Assert.Equal("First property", first.GetProperty("description").GetString());

        var second = properties.GetProperty("second");
        Assert.Equal("number", second.GetProperty("type").GetString());
        // Null descriptions become empty string
        Assert.Equal(string.Empty, second.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("first", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_ArrayProperty_SerializesWithArrayTypeAndItems()
    {
        var arrayProp = CreateProperty("tags", "string", "Tags list", required: true, isArray: true);
        var scalarProp = CreateProperty("count", "number", null, required: false, isArray: false);
        var tool = CreateTool(arrayProp, scalarProp);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var tags = properties.GetProperty("tags");
        Assert.Equal("array", tags.GetProperty("type").GetString());

        var items = tags.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.Equal("Tags list", tags.GetProperty("description").GetString());

        var count = properties.GetProperty("count");
        Assert.Equal("number", count.GetProperty("type").GetString());
        Assert.False(count.TryGetProperty("items", out var _));

        Assert.Equal(string.Empty, count.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("tags", required);
    }
}
