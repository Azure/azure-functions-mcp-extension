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

        builder.Context.ResolvedToolProperties =
        [
            new ToolProperty("age", "integer", "age desc", true)
        ];

        builder.PatchPropertyBindings();

        Assert.Equal("integer", builder.Context.Bindings[1].JsonObject["propertyType"]?.ToString());
    }

    [Fact]
    public void PatchPropertyBindings_NonMatchingProperty_NotPatched()
    {
        var builder = CreateBuilder(
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Tool\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"unrelated\"}");

        builder.Context.ResolvedToolProperties =
        [
            new ToolProperty("age", "integer", "age desc", true)
        ];

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[1].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_NoResolvedProperties_Noop()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.PatchPropertyBindings();

        Assert.Null(builder.Context.Bindings[0].JsonObject["propertyType"]);
    }

    [Fact]
    public void PatchPropertyBindings_EmptyResolvedProperties_Noop()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");
        builder.Context.ResolvedToolProperties = [];

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

        builder.Context.ResolvedToolProperties =
        [
            new ToolProperty("name", "string", "name desc", true),
            new ToolProperty("age", "integer", "age desc", false)
        ];

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

        builder.Context.ResolvedToolProperties =
        [
            new ToolProperty("name", "string", "name desc", true),
            new ToolProperty("Tool", "string", "should not match trigger", false)
        ];

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
