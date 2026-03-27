using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class AddInputSchemaExtensionTests
{
    [Fact]
    public void AddInputSchema_WithExplicitSchema_SetsInputSchemaAndUseWorkerFlag()
    {
        var explicitSchema = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool", inputSchema: explicitSchema);

        builder.AddInputSchema(toolOptions);

        var binding = builder.Context.Bindings[0];
        Assert.Equal("true", binding.JsonObject["useWorkerInputSchema"]?.ToString());
        Assert.NotNull(binding.JsonObject["inputSchema"]);
        Assert.NotNull(builder.Context.ResolvedInputSchema);
    }

    [Fact]
    public void AddInputSchema_WithProperties_GeneratesSchemaFromProperties()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool", [new ToolProperty("x", "string", "desc", true)]);

        builder.AddInputSchema(toolOptions);

        var binding = builder.Context.Bindings[0];
        Assert.Equal("true", binding.JsonObject["useWorkerInputSchema"]?.ToString());
        Assert.NotNull(binding.JsonObject["inputSchema"]);
        var schema = JsonNode.Parse(binding.JsonObject["inputSchema"]!.GetValue<string>())!.AsObject();
        Assert.Equal("object", schema["type"]?.ToString());
        var props = schema["properties"]!.AsObject();
        Assert.True(props.ContainsKey("x"));
    }

    [Fact]
    public void AddInputSchema_ExplicitSchemaTakesPrecedenceOverProperties()
    {
        var explicitSchema = """{"type":"object","properties":{"explicit":{"type":"number"}},"required":[]}""";
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool",
            properties: [new ToolProperty("fromProp", "string", "desc", true)],
            inputSchema: explicitSchema);

        builder.AddInputSchema(toolOptions);

        var binding = builder.Context.Bindings[0];
        var schema = JsonNode.Parse(binding.JsonObject["inputSchema"]!.GetValue<string>())!.AsObject();
        var props = schema["properties"]!.AsObject();
        Assert.True(props.ContainsKey("explicit"));
        Assert.False(props.ContainsKey("fromProp"));
    }

    [Fact]
    public void AddInputSchema_NoOptionsOrAttributes_SetsUseWorkerFlagButNoInputSchema()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool");

        builder.AddInputSchema(toolOptions);

        var binding = builder.Context.Bindings[0];
        Assert.Equal("true", binding.JsonObject["useWorkerInputSchema"]?.ToString());
        Assert.False(binding.JsonObject.ContainsKey("inputSchema"));
        Assert.Null(builder.Context.ResolvedInputSchema);
    }

    [Fact]
    public void AddInputSchema_SkipsNonToolTriggerBindings()
    {
        var builder = CreateBuilder("{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}");
        var toolOptions = CreateToolOptions("file://test",
            inputSchema: """{"type":"object","properties":{},"required":[]}""");

        builder.AddInputSchema(toolOptions);

        Assert.False(builder.Context.Bindings[0].JsonObject.ContainsKey("inputSchema"));
        Assert.False(builder.Context.Bindings[0].JsonObject.ContainsKey("useWorkerInputSchema"));
    }

    [Fact]
    public void AddInputSchema_SkipsToolTriggerWithBlankIdentifier()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\" \"}");
        var toolOptions = CreateToolOptions(" ",
            inputSchema: """{"type":"object","properties":{},"required":[]}""");

        builder.AddInputSchema(toolOptions);

        Assert.False(builder.Context.Bindings[0].JsonObject.ContainsKey("inputSchema"));
    }

    [Fact]
    public void AddInputSchema_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        var result = builder.AddInputSchema(CreateToolOptions());

        Assert.Same(builder, result);
    }
}
