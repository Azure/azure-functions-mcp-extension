// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class ToolPropertyParserTests
{
    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_NoAttribute_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.NoToolPropertyAttribute))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.False(result);
    }

    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_WithAttribute_ReturnsTrue()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithToolPropertyAttribute))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.True(result);
        Assert.Equal("name", toolProperty.Name);
        Assert.Equal("The name", toolProperty.Description);
        Assert.True(toolProperty.IsRequired);
        Assert.Equal("string", toolProperty.Type);
    }

    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_OptionalParameter_ReturnsIsRequiredFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithOptionalToolProperty))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.True(result);
        Assert.False(toolProperty.IsRequired);
    }

    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_IntParameter_ReturnsIntegerType()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithIntProperty))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.True(result);
        Assert.Equal("integer", toolProperty.Type);
    }

    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_BoolParameter_ReturnsBooleanType()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithBoolProperty))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.True(result);
        Assert.Equal("boolean", toolProperty.Type);
    }

    [Fact]
    public void TryGetToolPropertyFromToolPropertyAttribute_ArrayParameter_SetsIsArrayTrue()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithArrayProperty))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertyFromToolPropertyAttribute(parameter, out var toolProperty);

        Assert.True(result);
        Assert.True(toolProperty.IsArray);
        Assert.Equal("string", toolProperty.Type);
    }

    [Fact]
    public void TryGetToolPropertiesFromToolTriggerAttribute_NoTriggerAttribute_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.NoToolPropertyAttribute))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var toolProperties);

        Assert.False(result);
        Assert.Empty(toolProperties);
    }

    [Fact]
    public void TryGetToolPropertiesFromToolTriggerAttribute_WithToolInvocationContext_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithToolInvocationContext))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var toolProperties);

        Assert.False(result);
    }

    [Fact]
    public void TryGetToolPropertiesFromToolTriggerAttribute_WithPoco_ReturnsPropertiesFromPoco()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithPoco))!;
        var parameter = method.GetParameters()[0];

        var result = ToolPropertyParser.TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var toolProperties);

        Assert.True(result);
        Assert.Equal(2, toolProperties.Count);
        Assert.Contains(toolProperties, p => p.Name == "Title");
        Assert.Contains(toolProperties, p => p.Name == "Content");
    }

    [Fact]
    public void TryGetToolPropertiesFromToolTriggerAttribute_PocoWithDescription_ExtractsDescription()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithPoco))!;
        var parameter = method.GetParameters()[0];

        ToolPropertyParser.TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var toolProperties);

        var titleProp = toolProperties.First(p => p.Name == "Title");
        Assert.Equal("The title", titleProp.Description);
    }

    [Fact]
    public void TryGetToolPropertiesFromToolTriggerAttribute_PocoWithRequired_SetsIsRequired()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.WithPoco))!;
        var parameter = method.GetParameters()[0];

        ToolPropertyParser.TryGetToolPropertiesFromToolTriggerAttribute(parameter, out var toolProperties);

        var titleProp = toolProperties.First(p => p.Name == "Title");
        Assert.True(titleProp.IsRequired);

        var contentProp = toolProperties.First(p => p.Name == "Content");
        Assert.False(contentProp.IsRequired);
    }

    [Fact]
    public void GetPropertiesJson_EmptyList_ReturnsEmptyArray()
    {
        var result = ToolPropertyParser.GetPropertiesJson([]);

        Assert.Equal("[]", result!.ToString());
    }

    [Fact]
    public void GetPropertiesJson_WithProperties_ReturnsJsonArray()
    {
        var properties = new List<ToolProperty>
        {
            new("name", "string", "The name", true),
            new("count", "integer", "The count", false)
        };

        var result = ToolPropertyParser.GetPropertiesJson(properties);
        var json = result!.ToString();

        Assert.Contains("\"propertyName\":\"name\"", json);
        Assert.Contains("\"propertyName\":\"count\"", json);
        Assert.Contains("\"propertyType\":\"string\"", json);
        Assert.Contains("\"propertyType\":\"integer\"", json);
    }

    private class TestClass
    {
        public void NoToolPropertyAttribute(string value) { }

        public void WithToolPropertyAttribute(
            [McpToolProperty("name", "The name", true)] string name) { }

        public void WithOptionalToolProperty(
            [McpToolProperty("name", "The name", false)] string name) { }

        public void WithIntProperty(
            [McpToolProperty("count", "The count", true)] int count) { }

        public void WithBoolProperty(
            [McpToolProperty("enabled", "Is enabled", true)] bool enabled) { }

        public void WithArrayProperty(
            [McpToolProperty("items", "The items", true)] string[] items) { }

        public void WithToolInvocationContext(
            [McpToolTrigger("test", "desc")] ToolInvocationContext context) { }

        public void WithPoco(
            [McpToolTrigger("test", "desc")] TestPoco poco) { }
    }

    public class TestPoco
    {
        [Description("The title")]
        [Required]
        public required string Title { get; set; }

        [Description("The content")]
        public string? Content { get; set; }
    }
}
