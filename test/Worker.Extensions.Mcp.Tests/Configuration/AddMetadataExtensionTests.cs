using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class AddMetadataExtensionTests : IDisposable
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
    }

    [Fact]
    public void AddMetadata_FluentMetadataAppliedToToolTrigger()
    {
        var toolOptions = CreateToolOptions("MyTool", metadata: new Dictionary<string, object> { ["author"] = "Jane" });
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        builder.AddMetadata();

        var metadata = builder.Context.Bindings[0].Metadata;
        Assert.NotNull(metadata);
        Assert.Contains("author", metadata.ToJsonString());
    }

    [Fact]
    public void AddMetadata_FluentMetadataAppliedToResourceTrigger()
    {
        var resourceOptions = CreateResourceOptions("file://test", new Dictionary<string, object> { ["source"] = "local" });
        var builder = CreateBuilder(CreateToolOptions(), resourceOptions, CreatePromptOptions(), "{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\"}");

        builder.AddMetadata();

        var metadata = builder.Context.Bindings[0].Metadata;
        Assert.NotNull(metadata);
        Assert.Contains("source", metadata.ToJsonString());
    }

    [Fact]
    public void AddMetadata_FluentMetadataAppliedToPromptTrigger()
    {
        var promptOptions = CreatePromptOptions("MyPrompt", metadata: new Dictionary<string, object> { ["version"] = "1.0" });
        var builder = CreateBuilder(CreateToolOptions(), CreateResourceOptions(), promptOptions, "{\"type\":\"mcpPromptTrigger\",\"promptName\":\"MyPrompt\"}");

        builder.AddMetadata();

        var metadata = builder.Context.Bindings[0].Metadata;
        Assert.NotNull(metadata);
        Assert.Contains("version", metadata.ToJsonString());
    }

    [Fact]
    public void AddMetadata_SkipsToolPropertyBindings()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}");

        builder.AddMetadata();

        Assert.Null(builder.Context.Bindings[0].Metadata);
    }

    [Fact]
    public void AddMetadata_NoFluentNoAttribute_NoMetadataAdded()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        builder.AddMetadata();

        Assert.Null(builder.Context.Bindings[0].Metadata);
    }

    [Fact]
    public void AddMetadata_AttributedMetadataAppliedToToolTrigger()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolMetadata));
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", outputDir);

        var fn = CreateFunctionMetadata(
            entryPoint: entryPoint,
            scriptFile: scriptFile,
            name: "Func",
            bindings: new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolMetadata\"}" });

        var builder = new McpBindingBuilder(fn.Object, NullLogger.Instance, CreateToolOptions(), CreateResourceOptions(), CreatePromptOptions());

        builder.AddMetadata();

        var metadata = builder.Context.Bindings[0].Metadata;
        Assert.NotNull(metadata);
        Assert.Contains("author", metadata.ToJsonString());
    }

    [Fact]
    public void AddMetadata_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder("{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}");

        var result = builder.AddMetadata();

        Assert.Same(builder, result);
    }

    internal class TestFunctions
    {
        public void WithToolMetadata(
            [McpToolTrigger("WithToolMetadata", "desc")]
            [McpMetadata("""{"author": "Jane Doe"}""")] ToolInvocationContext context) { }
    }
}
