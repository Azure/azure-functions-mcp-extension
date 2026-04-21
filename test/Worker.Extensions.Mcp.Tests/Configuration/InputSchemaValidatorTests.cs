// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class InputSchemaValidatorTests
{
    [Fact]
    public void ValidateAndParse_ValidObjectSchema_ReturnsNode()
    {
        var json = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";

        var node = InputSchemaValidator.ValidateAndParse(json, "schema");

        Assert.NotNull(node);
        Assert.IsType<JsonObject>(node);
    }

    [Fact]
    public void ValidateAndParse_EmptyObjectSchema_Succeeds()
    {
        var node = InputSchemaValidator.ValidateAndParse("""{"type":"object"}""", "schema");
        Assert.NotNull(node);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndParse_NullOrWhitespace_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => InputSchemaValidator.ValidateAndParse(value!, "schema"));
    }

    [Fact]
    public void ValidateAndParse_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => InputSchemaValidator.ValidateAndParse("{not json", "schema"));
    }

    [Fact]
    public void ValidateAndParse_NotAnObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => InputSchemaValidator.ValidateAndParse("[1,2,3]", "schema"));
    }

    [Fact]
    public void ValidateAndParse_MissingType_Throws()
    {
        Assert.Throws<ArgumentException>(() => InputSchemaValidator.ValidateAndParse("""{"properties":{}}""", "schema"));
    }

    [Fact]
    public void ValidateAndParse_TypeNotObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => InputSchemaValidator.ValidateAndParse("""{"type":"string"}""", "schema"));
    }

    [Fact]
    public void ValidateAndParse_PropertiesNotObject_Throws()
    {
        Assert.Throws<ArgumentException>(() => InputSchemaValidator.ValidateAndParse("""{"type":"object","properties":[]}""", "schema"));
    }

    [Fact]
    public void ValidateAndParse_RequiredNotArray_Throws()
    {
        Assert.Throws<ArgumentException>(() => InputSchemaValidator.ValidateAndParse("""{"type":"object","required":"x"}""", "schema"));
    }

    [Fact]
    public void Validate_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => InputSchemaValidator.Validate(null!, "schema"));
    }
}
