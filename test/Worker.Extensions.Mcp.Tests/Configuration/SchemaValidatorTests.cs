// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class SchemaValidatorTests
{
    [Fact]
    public void ValidateAndParse_ValidObjectSchema_ReturnsNode()
    {
        var json = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";

        var node = SchemaValidator.ValidateAndParse(json, "Input", "schema");

        Assert.NotNull(node);
        Assert.IsType<JsonObject>(node);
    }

    [Fact]
    public void ValidateAndParse_EmptyObjectSchema_Succeeds()
    {
        var node = SchemaValidator.ValidateAndParse("""{"type":"object"}""", "Input", "schema");
        Assert.NotNull(node);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndParse_NullOrWhitespace_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => SchemaValidator.ValidateAndParse(value!, "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => SchemaValidator.ValidateAndParse("{not json", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_NotAnObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("[1,2,3]", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_MissingType_Throws()
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("""{"properties":{}}""", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_TypeNotObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("""{"type":"string"}""", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_PropertiesNotObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("""{"type":"object","properties":[]}""", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_RequiredNotArray_Throws()
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("""{"type":"object","required":"x"}""", "Input", "schema"));
    }

    [Fact]
    public void Validate_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SchemaValidator.Validate(null!, "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_PropertiesJsonNull_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SchemaValidator.ValidateAndParse("""{"type":"object","properties":null}""", "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_RequiredJsonNull_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SchemaValidator.ValidateAndParse("""{"type":"object","required":null}""", "Input", "schema"));
    }

    // Pins current behavior: element types within "required" are not validated by the worker.
    // The host silently drops non-string elements in GetRequiredProperties, so these schemas pass
    // structural validation but the non-string entries will be ignored at runtime.
    [Theory]
    [InlineData("""{"type":"object","required":[1,2]}""")]
    [InlineData("""{"type":"object","required":[true]}""")]
    [InlineData("""{"type":"object","required":[null]}""")]
    [InlineData("""{"type":"object","required":[{}]}""")]
    public void ValidateAndParse_RequiredWithNonStringElements_CurrentlyAllowed(string json)
    {
        var node = SchemaValidator.ValidateAndParse(json, "Input", "schema");
        Assert.NotNull(node);
    }

    [Theory]
    [InlineData("""{"type":null}""")]
    [InlineData("""{"type":5}""")]
    [InlineData("""{"type":"Object"}""")]
    [InlineData("""{"type":["object"]}""")]
    [InlineData("""{"type":true}""")]
    [InlineData("""{"type":{}}""")]
    public void ValidateAndParse_TypeVariants_Throws(string json)
    {
        Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse(json, "Input", "schema"));
    }

    [Fact]
    public void ValidateAndParse_OutputKind_UsesOutputInMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => SchemaValidator.ValidateAndParse("[]", "Output", "schema"));
        Assert.Contains("Output schema", ex.Message);
    }
}
