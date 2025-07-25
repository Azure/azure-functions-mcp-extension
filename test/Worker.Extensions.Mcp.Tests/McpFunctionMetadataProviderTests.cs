using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpFunctionMetadataProviderTests
{
    [Fact]
    public async Task GetFunctionMetadataAsync_InjectsToolProperties_FromMcpToolProperty()
    {
        // Arrange: setup test app with a function method and attributes
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunction>(nameof(TestFunction.GetSnippet));
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", outputDir);

        var provider = CreateFunctionMetadataProvider(
            entryPoint,
            scriptFile,
            "TestFunction",
            toolName: "testTool"
        );

        // Act
        var result = await provider.GetFunctionMetadataAsync(string.Empty);
        var metadata = result.SingleOrDefault();
        Assert.NotNull(metadata);

        if (metadata?.RawBindings?.Count > 0 && metadata.RawBindings[0] is string binding)
        {
            var json = JsonNode.Parse(binding);
            Assert.NotNull(json);
            var toolPropertiesJson = json["toolProperties"]!.GetValue<string>();
            Assert.Equal("[{\"propertyName\":\"name\",\"propertyType\":\"string\",\"description\":\"Name of the snippet to retrieve\",\"required\":true}]", toolPropertiesJson);
        }
        else
        {
            Assert.Fail("Metadata or RawBindings is null or empty.");
        }
    }

    [Fact]
    public async Task GetFunctionMetadataAsync_InjectsToolProperties_FromMcpToolTrigger()
    {
        // Arrange: setup test app with a function method and attributes
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunction>(nameof(TestFunction.SaveSnippet));
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", outputDir);

        var provider = CreateFunctionMetadataProvider(
            entryPoint,
            scriptFile,
            "TestFunction",
            toolName: "testTool"
        );

        // Act
        var result = await provider.GetFunctionMetadataAsync(string.Empty);
        var metadata = result.SingleOrDefault();
        Assert.NotNull(metadata);

        if (metadata?.RawBindings?.Count > 0 && metadata.RawBindings[0] is string binding)
        {
            var json = JsonNode.Parse(binding);
            Assert.NotNull(json);
            var toolPropertiesJson = json["toolProperties"]!.GetValue<string>();
            Assert.Equal("[{\"propertyName\":\"Content\",\"propertyType\":\"string\",\"description\":\"The content of the snippet\",\"required\":false},{\"propertyName\":\"Name\",\"propertyType\":\"string\",\"description\":\"The name of the snippet\",\"required\":true}]", toolPropertiesJson);
        }
        else
        {
            Assert.Fail("Metadata or RawBindings is null or empty.");
        }
    }

    private static (string EntryPoint, string ScriptFile, string OutputDir) GetFunctionMetadataInfo<T>(string methodName)
    {
        var type = typeof(T);
        var entryPoint = $"{type.FullName}.{methodName}";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;

        return (entryPoint, scriptFile, outputDir);
    }

    private static McpFunctionMetadataProvider CreateFunctionMetadataProvider(
        string entryPoint,
        string scriptFile,
        string functionName,
        string toolName = "testTool",
        List<ToolProperty>? toolProperties = null)
    {
        var mockInner = new Mock<IFunctionMetadataProvider>();
        var functionMetadata = new Mock<IFunctionMetadata>();
        functionMetadata.SetupGet(f => f.EntryPoint).Returns(entryPoint);
        functionMetadata.SetupGet(f => f.ScriptFile).Returns(scriptFile);
        functionMetadata.SetupGet(f => f.Name).Returns(functionName);
        functionMetadata.SetupGet(f => f.RawBindings).Returns(new List<string> {
            $"{{\"type\":\"mcpToolTrigger\",\"toolName\":\"{toolName}\"}}"
        });
        mockInner.Setup(i => i.GetFunctionMetadataAsync(It.IsAny<string>()))
            .ReturnsAsync([functionMetadata.Object]);

        var mockOptions = new Mock<IOptionsSnapshot<ToolOptions>>();
        mockOptions.Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new ToolOptions { Properties = toolProperties ?? new List<ToolProperty>() });

        return new McpFunctionMetadataProvider(mockInner.Object, mockOptions.Object);
    }

    internal class TestFunction
    {
        public class NameClass
        {
            [Description("The name of the snippet")]
            public required string Name { get; set; }
        }

        public class Snippet : NameClass
        {
            [Description("The content of the snippet")]
            public string? Content { get; set; }
        }

        public void SaveSnippet(
            [McpToolTrigger("SaveSnippet", "Save a snippet")] Snippet snippet) { }

        public void GetSnippet(
            [McpToolTrigger("GetSnippet", "Get a snippet")] ToolInvocationContext context,
            [McpToolProperty("name", "string", "Name of the snippet to retrieve", true)] string name) { }
    }
}
