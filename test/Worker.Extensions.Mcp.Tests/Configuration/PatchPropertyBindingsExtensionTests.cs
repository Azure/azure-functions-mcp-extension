using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class PatchPropertyBindingsExtensionTests
{
    [Fact]
    public void PatchPropertyBindings_MatchingProperty_SetsPropertyType()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}");

        builder.Context.ResolvedInputSchema = JsonNode.Parse(
            """{"type":"object","properties":{"age":{"type":"integer","description":"age desc"}},"required":["age"]}""");

        builder.PatchPropertyBindings();

        Assert.Equal("integer", builder.Context.Bindings[1].JsonObject["propertyType"]?.ToString());
    }

    [Fact]
    public void PatchPropertyBindings_NonMatchingProperty_NotPatched()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"unrelated\"}");

        builder.Context.ResolvedInputSchema = JsonNode.Parse(
            """{"type":"object","properties":{"age":{"type":"integer","description":"age desc"}},"required":["age"]}""");

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[1].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_NoResolvedSchema_Noop()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[0].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_EmptySchemaProperties_Noop()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");
        builder.Context.ResolvedInputSchema = JsonNode.Parse(
            """{"type":"object","properties":{},"required":[]}""");

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[0].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_MultipleProperties_PatchesAllMatching()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"unknown\"}");

        builder.Context.ResolvedInputSchema = JsonNode.Parse(
            """{"type":"object","properties":{"name":{"type":"string","description":"name desc"},"age":{"type":"integer","description":"age desc"}},"required":["name"]}""");

        builder.PatchPropertyBindings();

        Assert.Equal("string", builder.Context.Bindings[1].JsonObject["propertyType"]?.ToString());
        Assert.Equal("integer", builder.Context.Bindings[2].JsonObject["propertyType"]?.ToString());
        Assert.Null(builder.Context.Bindings[3].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_OnlyPatchesToolPropertyBindings()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.Context.ResolvedInputSchema = JsonNode.Parse(
            """{"type":"object","properties":{"name":{"type":"string","description":"name desc"},"Tool":{"type":"string","description":"should not match"}},"required":["name"]}""");

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[0].JsonObject["propertyType"]);
        Assert.Equal("string", builder.Context.Bindings[1].JsonObject["propertyType"]?.ToString());
    }

    [Fact]
    public void PatchPropertyBindings_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        var result = builder.PatchPropertyBindings();

        Assert.Same(builder, result);
    }
}
