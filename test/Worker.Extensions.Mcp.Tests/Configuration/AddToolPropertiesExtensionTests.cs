using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class AddToolPropertiesExtensionTests : IDisposable
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
    }

    [Fact]
    public void AddToolProperties_WithConfiguredOptions_SetsToolProperties()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool", [new ToolProperty("x", "string", "desc", true)]);

        builder.AddToolProperties(toolOptions);

        var tp = builder.Context.Bindings[0].ToolProperties?.ToString();
        Assert.NotNull(tp);
        Assert.Contains("\"propertyName\":\"x\"", tp);
    }

    [Fact]
    public void AddToolProperties_SetsResolvedToolPropertiesOnContext()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool", [new ToolProperty("x", "string", "desc", true)]);

        builder.AddToolProperties(toolOptions);

        Assert.NotNull(builder.Context.ResolvedToolProperties);
        Assert.Single(builder.Context.ResolvedToolProperties);
        Assert.Equal("x", builder.Context.ResolvedToolProperties[0].Name);
    }

    [Fact]
    public void AddToolProperties_NoOptionsOrAttributes_DoesNotSetToolProperties()
    {
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", Path.GetDirectoryName(typeof(AddToolPropertiesExtensionTests).Assembly.Location));
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");
        var toolOptions = CreateToolOptions("MyTool");

        builder.AddToolProperties(toolOptions);

        Assert.Null(builder.Context.Bindings[0].ToolProperties);
        Assert.Null(builder.Context.ResolvedToolProperties);
    }

    [Fact]
    public void AddToolProperties_SkipsNonToolTriggerBindings()
    {
        var builder = CreateBuilder("{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}");
        var toolOptions = CreateToolOptions("file://test", [new ToolProperty("x", "string", "desc", true)]);

        builder.AddToolProperties(toolOptions);

        Assert.Null(builder.Context.Bindings[0].ToolProperties);
    }

    [Fact]
    public void AddToolProperties_SkipsToolTriggerWithBlankIdentifier()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\" \"}");
        var toolOptions = CreateToolOptions(" ", [new ToolProperty("x", "string", "desc", true)]);

        builder.AddToolProperties(toolOptions);

        Assert.Null(builder.Context.Bindings[0].ToolProperties);
    }

    [Fact]
    public void AddToolProperties_ReturnsSameBuilder_ForChaining()
    {
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", Path.GetDirectoryName(typeof(AddToolPropertiesExtensionTests).Assembly.Location));
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        var result = builder.AddToolProperties(CreateToolOptions());

        Assert.Same(builder, result);
    }
}
