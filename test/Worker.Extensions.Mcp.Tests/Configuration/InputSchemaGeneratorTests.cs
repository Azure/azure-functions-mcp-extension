// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

/// <summary>
/// Output shape MUST stay byte-equivalent to host
/// <c>Microsoft.Azure.Functions.Extensions.Mcp.Validation.PropertyBasedToolInputSchema.BuildSchemaElement</c>
/// so that flipping <c>useWorkerInputSchema</c> on .NET workers does not change
/// the schema seen by clients.
/// </summary>
public class InputSchemaGeneratorTests
{
    [Fact]
    public void GenerateFromToolProperties_Empty_EmitsObjectWithEmptyPropsAndRequired()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties([]);

        Assert.Equal("""{"type":"object","properties":{},"required":[]}""", schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_ScalarRequired_OrderTypeDescription()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
            [new ToolProperty("name", "string", "user name", isRequired: true)]);

        var expected =
            """{"type":"object","properties":{"name":{"type":"string","description":"user name"}},"required":["name"]}""";
        Assert.Equal(expected, schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_NullDescription_BecomesEmptyString()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
            [new ToolProperty("x", "string", description: null)]);

        Assert.Contains("\"description\":\"\"", schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_Array_WrapsTypeInItems()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
            [new ToolProperty("tags", "string", "tag list", isArray: true)]);

        var expected =
            """{"type":"object","properties":{"tags":{"type":"array","items":{"type":"string"},"description":"tag list"}},"required":[]}""";
        Assert.Equal(expected, schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_Enum_NonArray_AddsEnumBetweenTypeAndDescription()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
            [new ToolProperty("color", "string", "pick one", enumValues: ["red", "green", "blue"])]);

        var expected =
            """{"type":"object","properties":{"color":{"type":"string","enum":["red","green","blue"],"description":"pick one"}},"required":[]}""";
        Assert.Equal(expected, schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_Enum_Array_PutsEnumOnItems()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
            [new ToolProperty("colors", "string", "pick many", isArray: true, enumValues: ["red", "green"])]);

        var expected =
            """{"type":"object","properties":{"colors":{"type":"array","items":{"type":"string","enum":["red","green"]},"description":"pick many"}},"required":[]}""";
        Assert.Equal(expected, schema.ToJsonString());
    }

    [Fact]
    public void GenerateFromToolProperties_MixedRequired_PreservesOrderAndDeduplicates()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
        [
            new ToolProperty("a", "string", "", isRequired: true),
            new ToolProperty("b", "integer", "", isRequired: false),
            new ToolProperty("c", "boolean", "", isRequired: true),
        ]);

        var required = (JsonArray)schema["required"]!;
        Assert.Collection(required,
            n => Assert.Equal("a", n!.GetValue<string>()),
            n => Assert.Equal("c", n!.GetValue<string>()));
    }

    [Fact]
    public void GenerateFromToolProperties_SkipsBlankNames()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
        [
            new ToolProperty("", "string", "no name"),
            new ToolProperty("   ", "string", "blank"),
            new ToolProperty("ok", "string", "good"),
        ]);

        var props = (JsonObject)schema["properties"]!;
        Assert.Single(props);
        Assert.True(props.ContainsKey("ok"));
    }

    [Fact]
    public void GenerateFromToolProperties_DuplicateRequiredNames_ListedOnce()
    {
        var schema = InputSchemaGenerator.GenerateFromToolProperties(
        [
            new ToolProperty("a", "string", "", isRequired: true),
            new ToolProperty("a", "string", "", isRequired: true),
        ]);

        var required = (JsonArray)schema["required"]!;
        Assert.Single(required);
    }

    [Fact]
    public void GenerateFromToolProperties_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InputSchemaGenerator.GenerateFromToolProperties(null!));
    }
}
